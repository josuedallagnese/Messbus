FROM node:24.2.0-alpine

RUN apk update && apk add --no-cache openjdk11 bash

RUN npm install -g firebase-tools

WORKDIR /gcp-emulators
COPY . .
RUN chmod 755 /gcp-emulators/entrypoint.sh

RUN chmod 755 entrypoint.sh

ENV UI_PORT 4000
ENV FIRESTORE_PORT 8080
ENV FIRESTORE_WS_PORT 5005
ENV PUBSUB_PORT 8085

EXPOSE "$UI_PORT"
EXPOSE "$FIRESTORE_PORT"
EXPOSE "$FIRESTORE_WS_PORT"
EXPOSE "$PUBSUB_PORT"

ENTRYPOINT ["./entrypoint.sh"]
