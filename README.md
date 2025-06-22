# SphereSSL Tray App


<p align="center">
  <img src="https://github.com/kl3mta3/SphereSSL/blob/master/Images/SphereSSL_ICON.png" alt="SphereSSL Logo" width="300"/>
<h2 align="center">
<b>One cert manager to rule them all, one CA to find them, one browser to bring them all, and in encryption bind them.</b>
</h2>
</p>

> **The essential companion to SphereSSL—control, monitor, and interact with your cert manager from the Windows system tray.**

---

## What is the SphereSSL Tray App?

The **SphereSSL Tray App** is a lightweight, always-on Windows tray utility that bridges your SphereSSL server/web app with the local user environment.  
It’s designed to provide quick access, interactive features, and desktop integration that a web server alone can’t handle.

---

## Key Features

- **System Tray Control**  
  Easily access SphereSSL from the tray, monitor status, and get real-time notifications.

- **File Dialog Integration**  
  Open folder browsers or file pickers (for save paths, etc.) directly on the user’s desktop, regardless of web browser sandboxing.

- **Show File/Folder Location**  
  Quickly open folders or file locations in Windows Explorer, even when triggered from the web UI.

- **Notification Pop-ups**  
  Receive toasts for certificate renewals, errors, or important updates (no more missing a renewal because your browser was closed!).

- **Auto-start Option**  
  Optionally launch on Windows startup to ensure SphereSSL features are always available.

---

## How it Works

1. **Runs as a tray app on Windows**
2. **Communicates** securely with the SphereSSL web server via local IPC or web requests.
3. **Executes privileged or desktop-bound actions** (like opening folders, file dialogs, notifications) on behalf of the user.

---

## When Do I Need It?

- You want the SphereSSL server to **open local file browsers** for choosing save paths.
- You want **system notifications** for certificate events.
- You want to **open certificate file locations** from the web dashboard.

> The Tray App is required for certain desktop-level features that can’t be achieved from a browser alone (due to security restrictions).

---

## Installation

1. **Download** the latest release from the [Releases](https://github.com/kl3mta3/SphereSSL/releases) page.
2. **Run the installer** (or EXE) on your Windows machine.
3. On first launch, the Tray App should automatically connect to your SphereSSL server. If prompted, configure the server address in the app settings.
4. **(Optional)** Enable "Start with Windows" from the tray icon menu.

---

## Usage

- Right-click the tray icon for menu options:
  - **Open Dashboard** (launches SphereSSL in your default browser)
  - **Show Log Folder** (or similar actions)
  - **Quit**

- The app runs quietly in the background, only popping up when needed or when an event occurs.

---

## Troubleshooting

- If you don’t see the tray icon:
  - Check the Windows system tray “overflow” section.
  - Make sure the app is running (check Task Manager).
  - Restart the app or your PC if needed.

- If the web server can’t talk to the Tray App:
  - Check firewall settings.
  - Ensure both the Tray App and the SphereSSL server are running on the same machine (for now).

---

## Security

- All communication is **local**—the Tray App does not send data to the cloud.
- No sensitive data is stored except necessary tokens for secure local communication.

---

## License

SphereSSL and the Tray App are licensed under the MIT License.  
See [LICENSE](../LICENSE) for details.

---

## Contributing

Pull requests are welcome! If you find a bug or have a feature request, open an issue or submit a PR.

---

**SphereSSL: One cert manager to rule them all.**
