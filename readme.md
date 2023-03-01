docker build -t inc-bot-image -f Dockerfile .

docker create --name inc-bot inc-bot-image

docker start inc-bot

add the following values to your .env file:

 - TOKEN = discord token
 - STATE = path to the file you want it to save the current state to