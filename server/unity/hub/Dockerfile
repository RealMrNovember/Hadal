# One-off Unity Hub (GUI over noVNC) on top of the exact editor image, used only to activate a
# Personal license on this machine identity. Run via `unity.sh hub`, then open http://localhost:6080.
ARG EDITOR_IMAGE
FROM ${EDITOR_IMAGE}

RUN apt-get update \
 && apt-get install -y --no-install-recommends wget gnupg ca-certificates xvfb x11vnc fluxbox novnc websockify \
      xdg-utils desktop-file-utils \
 && wget -qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor -o /usr/share/keyrings/Unity_Technologies_ApS.gpg \
 && echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" \
      > /etc/apt/sources.list.d/unityhub.list \
 && apt-get update \
 && apt-get install -y --no-install-recommends unityhub \
 && wget -qO /tmp/chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
 && apt-get install -y --no-install-recommends /tmp/chrome.deb \
 && rm -rf /tmp/chrome.deb /var/lib/apt/lists/*

# Sign-in flow: Hub opens the system browser (Chrome), which hands the unityhub:// deep link back
# through xdg-open. Both Chrome and Hub's Electron need --no-sandbox as root, hence the wrappers.
COPY chrome-wrapper.sh /usr/local/bin/hub-chrome
COPY unityhub-wrapper.sh /usr/local/bin/unityhub
COPY hub-chrome.desktop /usr/share/applications/hub-chrome.desktop
COPY start-hub.sh /start-hub.sh
RUN chmod +x /start-hub.sh /usr/local/bin/hub-chrome /usr/local/bin/unityhub \
 && update-desktop-database /usr/share/applications \
 && for t in x-scheme-handler/http x-scheme-handler/https text/html; do xdg-mime default hub-chrome.desktop "$t"; done

EXPOSE 6080
CMD ["/start-hub.sh"]
