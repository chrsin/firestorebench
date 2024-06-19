#build and run it

docker compose up -d --build


#Compile Templates
helm template sally charts/ -f charts/Values.yaml