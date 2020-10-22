#!/bin/bash

if [[ -d "/home/vscode/.vscode-server/" ]]
then
    mv /home/vscode/.vscode-server/data/Machine/settings.json /home/vscode/.vscode-server/data/Machine/settings.backup.json
    cp /home/tmp/vscode-settings.json /home/vscode/.vscode-server/data/Machine/settings.json
else
    mkdir -p /home/vscode/.vscode-remote/data/Machine/
    cp /home/tmp/codespaces-settings.json /home/vscode/.vscode-remote/data/Machine/settings.json
    #chmod -R a=rwx /home/vscode/.vscode-remote/
    chown -R vscode /home/vscode/.vscode-remote/
fi
