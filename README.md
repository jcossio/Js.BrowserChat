# Js.BrowserChat
Browser chat implementation

This solution intends to create a Asp Net Core Browser Chat application using:

* RabbitMQ.
* SignalR.
* Asp Net Core Identity (Stored on LocalDB).
* External console app bot.

## Prerequisites

### Identity

For the identity support there needs to be a database update issuing a CLI command:

`dotnet ef database update`

This will execute the stored migrations to create the required tables to support the identity process.

In order for the user to join the chat, he/she needs to register or login to get access to the chat page.

### RabbitMQ

There is also a dependency to RabbitMQ to be installed on the local machine. Otherwise, the rabbitMQ parameter would need to be modified.
