import asyncio
import time

from azure.messaging.webpubsubclient import WebPubSubClient
from azure.messaging.webpubsubservice import WebPubSubServiceClient
from fastapi import FastAPI
from pydantic import BaseModel

import os
import aioredis
import json

connectionString = os.getenv('connectionString')
redisConnectionString = os.getenv("redis")
testId = os.getenv("testId")
podName = os.getenv("Podname")


class Data(BaseModel):
    Type: str
    Ticks: int
    Target: str
    Payload: str


class DataForMeasure:
    Ticks: int
    Payload: str


app = FastAPI()

service = WebPubSubServiceClient.from_connection_string(connection_string=connectionString, hub='hub')

token = service.get_client_access_token(roles=["webpubsub.joinLeaveGroup", "webpubsub.sendToGroup"],
                                        minutes_to_expire=60 * 24 * 365)
url = token["url"]
client = WebPubSubClient(url)
client.__enter__()


@app.post("/")
async def send(data: Data):
    print(data)
    if data.Type == "sendToGroup":
        dataForMeasure = DataForMeasure()

        dataForMeasure.Ticks = data.Ticks
        dataForMeasure.Payload = data.Payload

        client.send_to_group(data.Target, data, "json")
    else:
        print("Not supported type:" + data.Type)


def start_app():
    import uvicorn
    asyncio.run(report_ready())
    uvicorn.run(app, host="0.0.0.0", port=8080)


async def report_ready():
    redis = aioredis.from_url(f"redis://{redisConnectionString}:6379")

    channel = f"{testId}:Coordinator:ReportReady:Command"
    message = {
        "Sender": podName,
        "Command": "ReportReady",
        "AckId": 1,
        "Parameters": {
            "Role": "AppServers"
        }
    }
    await redis.publish(channel, json.dumps(message))
    time.sleep(2)


if __name__ == "__main__":
    start_app()
