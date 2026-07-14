//
const { contextBridge, ipcRenderer } = require('electron');

// React tarafında (window.electronAPI) üzerinden erişilecek fonksiyonlar
contextBridge.exposeInMainWorld('electronAPI', {
    ping: () => console.log("Electron'dan React'a selamlar! Köprü aktif.")
    // İleride native-bridge fonksiyonlarımızı buraya ekleyeceğiz.
});