import asyncio
import socket
import subprocess
import sys
import time
import uuid

from azure.messaging.webpubsubclient.aio import WebPubSubClient as WebPubSubClientAio
from azure.messaging.webpubsubservice.aio import WebPubSubServiceClient as WebPubSubServiceClientAio
from fastapi import FastAPI
from pydantic import BaseModel

import os
import json
from redis.asyncio import Redis

connectionString = os.getenv('connectionString')
instance_id = uuid.uuid4().hex

class Data(BaseModel):
    Type: str
    Ticks: int
    Target: str
    Payload: str


app = FastAPI()


class Client:
    _instance = None

    @classmethod
    async def get_instance(cls):
        if cls._instance is None:
            service = WebPubSubServiceClientAio.from_connection_string(connection_string=connectionString,
                                                                    hub="signalrbench")
            token = await service.get_client_access_token(roles=["webpubsub.joinLeaveGroup", "webpubsub.sendToGroup"],
                                                    minutes_to_expire=60 * 24 * 365)
            url = token["url"]
            cls._instance = WebPubSubClientAio(url)
            await cls._instance.open()
        return cls._instance


async def report_ready():
    test_id = os.getenv("testId")
    pod_name = os.getenv("Podname")
    redis_conn = os.getenv("redis")

    # create an asyncio-enabled Redis client
    client = Redis.from_url(f"redis://{redis_conn}:6379")

    channel = f"{test_id}:Coordinator:ReportReady:Command"
    message = {
        "Sender": pod_name,
        "Command": "ReportReady",
        "AckId": 1,
        "Parameters": {
            "Role": "AppServers",
            "InstanceId": instance_id
        }
    }

    # publish and wait
    await client.publish(channel, json.dumps(message))
    await asyncio.sleep(2)

    # clean up
    await client.close()

@app.post("/")
async def send(data: Data):
    print(data)
    if data.Type == "sendToGroup":
        data_for_measure = {
            "Ticks": data.Ticks,
            "Payload": data.Payload
        }
        client = await Client.get_instance()
        await client.send_to_group(data.Target, data_for_measure, "json", ack=True)
    else:
        print("Not supported type:" + data.Type)


def wait_until_listening(server, port, timeout_seconds=120):
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        if server.poll() is not None:
            raise RuntimeError(f"Server exited with code {server.returncode} before listening on port {port}")
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=1):
                return
        except OSError:
            time.sleep(0.2)
    raise TimeoutError(f"Server did not listen on port {port} within {timeout_seconds}s")


def start_app():
    import multiprocessing
    server = subprocess.Popen(["uvicorn", "pyserver.main:app", "--host", "0.0.0.0", "--port", "8080",
                               "--workers", str(multiprocessing.cpu_count())])
    try:
        # Ready must mean the server can serve, so report it only once uvicorn accepts connections.
        wait_until_listening(server, 8080)
        asyncio.run(report_ready())
    except BaseException:
        server.terminate()
        server.wait()
        raise
    sys.exit(server.wait())


if __name__ == "__main__":
    import uvicorn

    # asyncio.run(report_ready())
    uvicorn.run(app, host="0.0.0.0", port=8080)
