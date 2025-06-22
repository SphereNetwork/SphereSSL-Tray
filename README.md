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

### Option 1: Download Prebuilt Release

1. **Download** the latest release from the [Releases](https://github.com/kl3mta3/SphereSSL/releases) page.
2. **Run the installer** (or EXE) on your Windows machine.
3. On first launch, the Tray App should automatically connect to your SphereSSL server. If prompted, configure the server address in the app settings.
4. **(Optional)** Enable "Start with Windows" from the tray icon menu.

### Option 2: Building from Source

1. **Clone this repository**:
    ```bash
    git clone https://github.com/kl3mta3/SphereSSL.git
    ```
2. **Build the project** using Visual Studio or the .NET CLI.
3. **Locate the Tray App**:
   - After building, the **SphereSSL.TrayApp** executable will be available in the `/SphereSSL.TrayApp/bin/Release` (or `/Debug`) folder.
   - **Copy the built Tray App** into the main SphereSSL install directory
4. **Run `SphereSSL.exe`** on your Windows machine.

---

> **Note:**  
> The Tray App is built as part of the SphereSSL solution but must be run separately from the main web server. Make sure the Tray App is running for full desktop integration (file dialogs, notifications, explorer integration, etc).

---

## Usage

- Right-click the tray icon for menu options:
  - **Open Dashboard** (launches SphereSSL in your default browser)
  - **Restart Server** 
  - **Quit**

- The app runs quietly in Tray, only popping up when needed or when an event occurs.

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

**SphereSSL- Tray** is open source, licensed under the **Business Source License 1.1 (BSL 1.1)**

**Licensor:** Kenneth Lasyone (“Kenny”) / Sphere / SphereNetwork  
**Change Date:** None (license will not automatically convert to a more permissive license).

---

**Permission is hereby granted** to any individual or entity (“You”) to use, copy, modify, and self-host this software for **non-commercial purposes only**.

### You MAY NOT:
- Copy, distribute, or sublicense this software, in whole or in part, for any commercial purpose.
- Use this software, or any portion thereof, in any paid or revenue-generating service, SaaS, or product.
- Rebrand, resell, or otherwise profit from this software **without prior written consent from the Licensor**.

For any commercial use, or to obtain written permission, **contact:**  
`Kl3mta3@gmail.com`

Violation of these terms may result in revocation of this license and potential legal action.  
Sorry, not sorry! Don’t make me come over there.

---

> THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND.

[See the full Business Source License 1.1](https://mariadb.com/bsl11) for more details.

---

## Contributing

Pull requests are welcome! If you find a bug or have a feature request, open an issue or submit a PR.

---

**SphereSSL: One cert manager to rule them all.**
