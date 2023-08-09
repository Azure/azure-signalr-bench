import {Server} from "socket.io";

const {useAzureSocketIO} = require("@azure/web-pubsub-socket.io");

const AckTimeout = 10000;
const AckMessage = "AckMessage";
const EventMeasure = "Measure";
const EventMeasureWithAck = "MeasureWithAck";
const ServerNotifyAckReceived = "ServerNotifyAckReceived";

const testId = process.env.testId;
const redisConnectionString = process.env.redis;
const podName = process.env.Podname;
const connectionString = process.env.connectionString;


async function main() {
    const io = new Server(8080)

    const wpsOptions = {
        hub: "signalrbench",
        connectionString: connectionString,
        webPubSubServiceClientOptions: {allowInsecureConnection: true}
    };

    await useAzureSocketIO(io, wpsOptions);
    console.log("Server started");

    io.on("connection", (socket) => {
        console.log(`connect ${socket.id}`);

        socket.on("disconnect", () => {
            console.log(`disconnect ${socket.id}`);
        });

        socket.on("Echo", (time, payload, cb) => {
            io.to(socket.id).emit(EventMeasure, time, payload);
            cb?.(AckMessage);
            console.log("Echo")
        });

        socket.on("Broadcast", (time, payload, cb) => {
            io.emit(EventMeasure, time, payload)
            cb?.(AckMessage);
            console.log("Broadcast");
        });

        socket.on("JoinGroup", async (room, cb) => {
            await socket.join(room);
            cb?.(AckMessage);
            console.log("joinGroup")
        });
        socket.on("LeaveGroup", async (room, cb) => {
            await socket.leave(room);
            cb?.(AckMessage);
            console.log("LeaveGroup")
        });
        socket.on("SendToGroup", (room, time, payload, cb) => {
            io.to(room).emit(EventMeasure, time, payload);
            cb?.(AckMessage);
            console.log(`SendToGroup ${room}`)
        });

        socket.on("SendToSocket", (id, time, payload, cb) => {
            io.to(id).emit(EventMeasure, time, payload);
            cb?.(AckMessage);
            console.log(`SendToSocket ${id}`)
        });


        socket.on("EchoWithAck", async (time, payload, cb) => {
            io.timeout(AckTimeout).to(socket.id).emit(EventMeasureWithAck, time, payload, (err, response) => {
                if (err) {
                    console.log(err);
                } else {
                    cb?.(AckMessage);
                }
            });
        });

        socket.on("BroadcastWithAck", (time, payload, cb) => {
            io.timeout(AckTimeout).emit(EventMeasureWithAck, time, payload, (err, response) => {
                if (err) {
                    console.log(err);
                } else {
                    cb?.(AckMessage);
                    console.log("BroadcastWithAck");
                }
            });
        });

        socket.on("SendToGroupWithAck", (room, time, payload, cb) => {
            io.timeout(AckTimeout).to(room).emit(EventMeasureWithAck, time, payload, (err, response) => {
                if (err) {
                    console.log(err);
                } else {
                    cb?.(AckMessage);
                    console.log(`SendToGroupWithAck ${room}`)
                }
            });
        });

        socket.on("SendToSocketWithAck", (id, time, payload, cb) => {
            io.timeout(AckTimeout).to(id).emit(EventMeasureWithAck, time, payload, (err, response) => {
                if (err) {
                    console.log(err);
                } else {
                    cb?.(AckMessage);
                    console.log(`SendToSocketWithAck ${id}`)
                }
            });
        });
    });

    await reportReady();
}

async function reportReady() {
    console.log("Server ready, testId is: ", testId);
    const redis = require('redis');
    const client = redis.createClient({
        url: `redis://${redisConnectionString}:6379`
    });

    await client.connect()

    const channel = `${testId}:Coordinator:ReportReady:Command`;
    console.log("publish to channel: ", channel)
    const message = {
        "Sender": podName,
        "Command": "ReportReady",
        "AckId": 1,
        "Parameters": {
            "Role": "AppServers"
        }
    }
    console.log("Publishing message: ", message)
    await client.publish(channel, JSON.stringify(message));
}

main();

