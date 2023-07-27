FROM node:14

LABEL vendor="Blik JavaScript SDK example"

ADD . /

ARG CLIENT_ID
ARG APP_SECRET

ENV CLIENT_ID=$CLIENT_ID
ENV APP_SECRET=$APP_SECRET

RUN npm install --ignore-scripts

CMD node server/index.js
