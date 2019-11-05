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

## Usage

* Run the sample publisher console app to add dummy data to the system
* Run the Bot console app to be sensing for stock commands and to query the web for the stock price
* Run the Web app to register, login on internal DB using identity
* Chat messages are not automatically placed for now. You need to refresh the page.
* App still has issues with Rabbit MQ.
* Pendind and part of the plan was the use of SignalR.
