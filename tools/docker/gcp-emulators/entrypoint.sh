#!/bin/sh

if [ -z "$PROJECT_ID" ]; then
  echo "The PROJECT_ID is not defined."
  exit 1
fi

sed -i "s/%PROJECT_ID%/$PROJECT_ID/g" firebase.json
sed -i "s/%FIRESTORE_PORT_PLACEHOLDER%/$FIRESTORE_PORT/g" firebase.json
sed -i "s/%FIRESTORE_WSPORT_PLACEHOLDER%/$FIRESTORE_WS_PORT/g" firebase.json
sed -i "s/%UI_PORT_PLACEHOLDER%/$UI_PORT/g" firebase.json
sed -i "s/%PUBSUB_PORT%/$PUBSUB_PORT/g" firebase.json

cat firebase.json
firebase init emulators
firebase emulators:start --project "$PROJECT_ID"
