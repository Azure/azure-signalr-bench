import asyncio
import time

from azure.messaging.webpubsubclient.aio import WebPubSubClient as WebPubSubClientAio
from azure.messaging.webpubsubservice.aio import WebPubSubServiceClient as WebPubSubServiceClientAio
from fastapi import FastAPI
from pydantic import BaseModel

import os
import json
from redis.asyncio import Redis

connectionString = os.getenv('connectionString')

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
            "Role": "AppServers"
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


def start_app():
    import multiprocessing
    asyncio.run(report_ready())
    os.system(f"uvicorn pyserver.main:app --host 0.0.0.0 --port 8080 --workers {multiprocessing.cpu_count()}")


if __name__ == "__main__":
    import uvicorn

    # asyncio.run(report_ready())
    uvicorn.run(app, host="0.0.0.0", port=8080)
