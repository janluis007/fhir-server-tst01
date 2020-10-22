# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.140.1/containers/dotnetcore/.devcontainer/base.Dockerfile

# [Choice] .NET Core version: 3.1, 2.1
ARG VARIANT="3.1"
FROM mcr.microsoft.com/vscode/devcontainers/dotnetcore:0-${VARIANT}

# [Option] Install Node.js
ARG INSTALL_NODE="true"
ARG NODE_VERSION="lts/*"
RUN if [ "${INSTALL_NODE}" = "true" ]; then su vscode -c "source /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi

# [Option] Install Azure CLI
ARG INSTALL_AZURE_CLI="false"
COPY library-scripts/azcli-debian.sh /tmp/library-scripts/
RUN if [ "$INSTALL_AZURE_CLI" = "true" ]; then bash /tmp/library-scripts/azcli-debian.sh; fi \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/* /tmp/library-scripts

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <packages>

# [Optional] Uncomment this line to install global node packages.
RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g azurite" 2>&1

# RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
# RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf

RUN mkdir -m a=rwx -p /home/tmp
COPY library-scripts/dotnet-install.sh /home/tmp
COPY vscode-settings.json /home/tmp
COPY codespaces-settings.json /home/tmp
COPY setup.sh /home/tmp
RUN chmod a=rwx /home/tmp/*

RUN /home/tmp/dotnet-install.sh -c 3.1 -v 3.1.403