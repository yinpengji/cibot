# CIBot - CIBot created for Skype for business
A bot to automatically respond to Skype queris for CI status


### Description
Try to make the CI interactive, make a CI robot to answer the frequently asked CI questions.

### Features

* Responds to greetings - Hi, Hello, Hey.
* Responds for queris like current build, build status, successful nightly build.
* Send a image to the committer when there's a failure CI build, this will require you have such info in your CI server info.


### Dependencies

You need to put your CI info to some server. In the current implementation, it reads the JSON response from your CI info database.
Of course you can try to use the Jeckins info JSON. You'll need to change the URL in the code

### How to use

The bot runs within the console applications. And it will need the Lync installed on your computer.
First create a new [LUIS](https://www.luis.ai/) application by importing the model json from `LuisModel` directory. Copy your LUIS model id and subscription key and paste it in `LuisModel` attribute in `LyncLuisDialog.cs`.  


You'd better to create an account for your CI bot, and make sure the skype account and the bot is running on the same machine.
