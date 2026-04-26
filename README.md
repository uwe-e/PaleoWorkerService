# PaleoWorkerService

I need two tickets for Paleo Thursday. This service is designed to monitor the website https://bourse.paleo.ch for available tickets. A request is sent every 10 minutes to check if a ticket is available. If one is available, the service should send an email to the configured email address.

## Publish the Worker Service

To install your .NET Worker Service as a Windows Service on another computer, follow these steps:

On your development machine, publish the project to a folder:

`dotnet publish -c Release -r win-x64 --self-contained true -o C:\Publish\PaleoWorkerService`

### Copy Published Files

Copy the contents of the published folder (C:\Publish\PaleoWorkerService) to the target computer, e.g., C:\Services\PaleoWorkerService.

### Install the Service

On the target computer, open an elevated Command Prompt (Run as Administrator) and run:

`sc create PaleoWorkerService binPath= "C:\Services\PaleoWorkerService\PaleoWorkerService.exe"`

### Start the Service

Start the service with:

`sc start PaleoWorkerService`

### Stop the Service

`sc stop PaleoWorkerService`

### Delete the Service

`sc delete PaleoWorkerService`

## Configure the EMail Settings as Environment variables

Via Windows UI:

1.	Open System Properties → Advanced → Environment Variables.
2.	Under "System variables", click "New..." and add each variable (e.g., EmailSettings__Username).
3.	Click OK and restart your service.

Via PowerShell:

[System.Environment]::SetEnvironmentVariable("EmailSettings__Username", "username@adress.com", "Machine")
[System.Environment]::SetEnvironmentVariable("EmailSettings__SmtpPort", "587", "Machine")
[System.Environment]::SetEnvironmentVariable("EmailSettings__SmtpHost", "smtp.yourprovider.com", "Machine")
[System.Environment]::SetEnvironmentVariable("EmailSettings__Password", "your-password", "Machine")
[System.Environment]::SetEnvironmentVariable("EmailSettings__FromEmail", "username@adress.com", "Machine")
[System.Environment]::SetEnvironmentVariable("EmailSettings__ToEmail", "recipientname@adress.com", "Machine")
