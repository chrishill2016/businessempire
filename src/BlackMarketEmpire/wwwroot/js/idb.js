// Minimal IndexedDB key/value store used by IndexedDbStore.cs
const DB_NAME = 'black-market-empire';
const STORE = 'kv';
let dbPromise;

function openDb() {
    dbPromise ??= new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, 1);
        req.onupgradeneeded = () => req.result.createObjectStore(STORE);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
    return dbPromise;
}

async function run(mode, action) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, mode);
        const req = action(tx.objectStore(STORE));
        req.onsuccess = () => resolve(req.result ?? null);
        req.onerror = () => reject(req.error);
    });
}

export const get = key => run('readonly', s => s.get(key));
export const set = (key, value) => run('readwrite', s => s.put(value, key));
export const del = key => run('readwrite', s => s.delete(key));
