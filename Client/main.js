const { app, BrowserWindow } = require('electron');
const path = require('path');

let mainWindow;

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1440,
        height: 900,
        minWidth: 1024,
        minHeight: 768,
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            contextIsolation: true,
            nodeIntegration: false,
        },
        title: "FraudGuard Analist Paneli"
    });

    const isDev = process.env.NODE_ENV === 'development';

    if (isDev) {
        // Geliştirme modunda Vite'ın çalıştığı adresi yükle
        mainWindow.loadURL('http://localhost:5173');
        mainWindow.webContents.openDevTools(); // Sağda konsolu otomatik açar
    } else {
        // Canlıya alırken (build) React'ın derlenmiş halini okuyacak
        mainWindow.loadFile(path.join(__dirname, 'react-ui/dist/index.html'));
    }
}

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
});