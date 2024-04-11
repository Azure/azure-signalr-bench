from azure.messaging.webpubsubclient import WebPubSubClient
from azure.messaging.webpubsubservice import WebPubSubServiceClient
from fastapi import FastAPI
from pydantic import BaseModel


class Data(BaseModel):
    Type: str
    Ticks: int
    Target: str
    Payload: str


# class Client:
#     _instance = None
#
#     def __new__(cls):
#         if cls._instance is None:
#             cls._instance = super().__new__(cls)
#             # Initialize your resource here (e.g., db connection)
#             cls._instance.initialize()
#         return cls._instance
#
#     def initialize(self):
#         # Initialize the resource (e.g., connect to the database)
#         self.connection = "Database connection"


app = FastAPI()

service = WebPubSubServiceClient.from_connection_string(connection_string=connectionString, hub='hub')

token = service.get_client_access_token(roles=["webpubsub.joinLeaveGroup", "webpubsub.sendToGroup"],
                                        minutes_to_expire=60 )
url = token["url"]
client = WebPubSubClient(url)
client.__enter__()

@app.post("/")
async def send(data: Data):
    print(data)
    if data.Type == "sendToGroup":
        client.send_to_group(data.Target, "hello world", "text")
        print("sendToGroup")
    else:
        print("Not supported type:" + data.Type)


def start_app():
    """Function to run the FastAPI app."""
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)


if __name__ == "__main__":
    start_app()
