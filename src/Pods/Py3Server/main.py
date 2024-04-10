
connectionString="xxx"

from azure.messaging.webpubsubclient import WebPubSubClient
from azure.messaging.webpubsubservice import WebPubSubServiceClient
import time
from fastapi import FastAPI

class Client:
    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            # Initialize your resource here (e.g., db connection)
            cls._instance.initialize()
        return cls._instance

    def initialize(self):
        # Initialize the resource (e.g., connect to the database)
        self.connection = "Database connection"

# Usage within a FastAPI endpoint
db_instance = DatabaseConnection()

app = FastAPI()

@app.get("/")
async def root():
    return {"message": "Hello World"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)

# if __name__ == '__main__':
#     print_hi('PyCharm')
#
#     service = WebPubSubServiceClient.from_connection_string(connection_string=connectionString, hub='hub')
#
#     service.send_to_all(message={
#         'from': 'user1',
#         'data': 'Hello world'
#     })
#     token=service.get_client_access_token(roles=["webpubsub.joinLeaveGroup","webpubsub.sendToGroup"])
#     print_hi(token)
#     print_hi(token["url"])
#     url=token["url"]
#     client = WebPubSubClient(url)
#     with client:
#         group_name = "group1";
#         client.subscribe("group-message", lambda e: print(f"Received message: {e.data}"));
#         client.join_group(group_name);
#         client.send_to_group(group_name, "hello world", "text");
