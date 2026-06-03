import { HubConnectionBuilder } from '@microsoft/signalr'
let connection = null
export function createConnection(getToken) {
  connection = new HubConnectionBuilder()
    .withUrl('/chat', { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build()
  return connection
}
export async function startConnection() { await connection.start() }
export function onReceive(handler) { connection.on('ReceiveMessage', handler) }
export async function joinRoom(id){ await connection.invoke('JoinRoom', id) }
export async function sendMessage(text){ await connection.invoke('SendMessage', text) }