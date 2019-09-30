// The following sample code uses TypeScript and must be compiled to JavaScript
// before a browser can execute it.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};

function generateRandomName() {
    return Math.random().toString(36).substring(2, 10);
}

// Get the user name and store it to prepend to messages.
var username = generateRandomName();
var promptMessage = 'Enter your name:';
do {
    username = prompt(promptMessage, username);
    if (!username || username.startsWith('_') || username.indexOf('<') > -1 || username.indexOf('>') > -1) {
        username = '';
        promptMessage = 'Invalid input. Enter your name:';
    }
} while (!username)

// Set initial focus to message input box.
var messageInput = document.getElementById('message');
messageInput.focus();

function createMessageEntry(encodedName, encodedMsg) {
    var entry = document.createElement('div');
    entry.classList.add("message-entry");
    if (encodedName === "_SYSTEM_") {
        entry.innerHTML = encodedMsg;
        entry.classList.add("text-center");
        entry.classList.add("system-message");
    } else if (encodedName === "_BROADCAST_") {
        entry.classList.add("text-center");
        entry.innerHTML = '<div class="text-center broadcast-message">' + encodedMsg + '</div>';
    } else if (encodedName === username) {
        entry.innerHTML = '<div class="message-avatar pull-right">' + encodedName + '</div>' +
            '<div class="message-content pull-right">' + encodedMsg + '<div>';
    } else {
        entry.innerHTML = '<div class="message-avatar pull-left">' + encodedName + '</div>' +
            '<div class="message-content pull-left">' + encodedMsg + '<div>';
    }
    return entry;
}

function bindConnectionMessage(connection) {
    var messageCallback = function (name, message) {
        if (!message) return;
        // Html encode display name and message.
        var encodedName = name;
        var encodedMsg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        var messageEntry = createMessageEntry(encodedName, encodedMsg);

        var messageBox = document.getElementById('messages');
        messageBox.appendChild(messageEntry);
        messageBox.scrollTop = messageBox.scrollHeight;
    };
    // Create a function that the hub can call to broadcast messages.
    connection.on('broadcastMessage', messageCallback);
    connection.on('echo', messageCallback);
    connection.on('PleasePullStream', function () {
        console.log("receive pull stream notification");
        connection.stream("PullStream")
            .subscribe({
                next: (item) => {
                    var li = document.createElement("li");
                    li.textContent = item;
                    document.getElementById("messagesList").appendChild(li);
                },
                complete: () => {
                    var li = document.createElement("li");
                    li.textContent = "Stream completed";
                    document.getElementById("messagesList").appendChild(li);
                },
                error: (err) => {
                    var li = document.createElement("li");
                    li.textContent = err;
                    document.getElementById("messagesList").appendChild(li);
                },
            });
    });
    connection.onclose(onConnectionError);
}

function onConnectionError(error) {
    if (error && error.message) {
        console.error(error.message);
    }
    var modal = document.getElementById('myModal');
    modal.classList.add('in');
    modal.style = 'display: block;';
}

var connectionId = "";
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/streamHub")
    .build();
connection.on("ConnectionId", function (message) {
    connectionId = message;
    console.log(message);
});
bindConnectionMessage(connection);

document.getElementById("streamButton").addEventListener("click", (event) => __awaiter(this, void 0, void 0, function* () {
    try {
        connection.stream("Counter", 10, 500)
            .subscribe({
            next: (item) => {
                var li = document.createElement("li");
                li.textContent = item;
                document.getElementById("messagesList").appendChild(li);
            },
            complete: () => {
                var li = document.createElement("li");
                li.textContent = "Stream completed";
                document.getElementById("messagesList").appendChild(li);
            },
            error: (err) => {
                var li = document.createElement("li");
                li.textContent = err;
                document.getElementById("messagesList").appendChild(li);
            },
        });
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
}));
document.getElementById("uploadButton").addEventListener("click", (event) => __awaiter(this, void 0, void 0, function* () {
    const subject = new signalR.Subject();
    yield connection.send("UploadStream", subject);
    var iteration = 0;
    const intervalHandle = setInterval(() => {
        iteration++;
        subject.next(iteration.toString());
        if (iteration === 10) {
            clearInterval(intervalHandle);
            subject.complete();
        }
    }, 500);
    event.preventDefault();
}));
document.getElementById("p2pButton").addEventListener("click", (event) => __awaiter(this, void 0, void 0, function* () {
    const subject = new signalR.Subject();
    yield connection.send("SendStreamToClient", subject, connectionId);
    var iteration = 10;
    const intervalHandle = setInterval(() => {
        iteration++;
        console.log("sending " + iteration.toString());
        subject.next(iteration.toString());
        if (iteration === 20) {
            clearInterval(intervalHandle);
            subject.complete();
        }
    }, 500);
    event.preventDefault();
}));

function onConnected(connection) {
    console.log('connection started');
    //connection.invoke('broadcastMessage', '_SYSTEM_', username + ' JOINED');
    document.getElementById('sendmessage').addEventListener('click', function (event) {
        // Call the broadcastMessage method on the hub.
        if (messageInput.value) {
            connection.send('broadcastMessage', username, messageInput.value);
        }

        // Clear text box and reset focus for next comment.
        messageInput.value = '';
        messageInput.focus();
        event.preventDefault();
    });
    document.getElementById('message').addEventListener('keypress', function (event) {
        if (event.keyCode === 13) {
            event.preventDefault();
            document.getElementById('sendmessage').click();
            return false;
        }
    });
    document.getElementById('echo').addEventListener('click', function (event) {
        // Call the echo method on the hub.
        connection.send('echo', username, messageInput.value);

        // Clear text box and reset focus for next comment.
        messageInput.value = '';
        messageInput.focus();
        event.preventDefault();
    });
}

// We need an async function in order to use await, but we want this code to run immediately,
// so we use an "immediately-executed async function"
(() => __awaiter(this, void 0, void 0, function* () {
    try {
        yield connection.start().then(function () {
            onConnected(connection);
        });
    }
    catch (e) {
        console.error(e.toString());
    }
}))();
