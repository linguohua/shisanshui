package gameserver

import (
	"net/http"
	"shisanshui/config"
	"shisanshui/tables"
	token "shisanshui/token"
	"strconv"

	"shisanshui/xproto"

	"github.com/gorilla/websocket"
	"github.com/julienschmidt/httprouter"
	log "github.com/sirupsen/logrus"
)

const (
	wsReadLimit       = 2048 // 2k
	wsReadBufferSize  = 2048 // 2k
	wsWriteBufferSize = 4096 // 4k
)

var (
	upgrader = websocket.Upgrader{ReadBufferSize: wsReadBufferSize, WriteBufferSize: wsWriteBufferSize}
)

func acceptWebsocket(w http.ResponseWriter, r *http.Request, params httprouter.Params) {
	cl := log.WithField("peer", retrieveClientAddr(r))

	ws, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		cl.Println(err)
		return
	}

	playerType := params.ByName("playertype")
	q := r.URL.Query()

	// ensure websocket connection will be closed anyway
	defer ws.Close()
	// read limit
	ws.SetReadLimit(wsReadLimit)

	cl.Println("accept websocket:", r.URL)
	switch playerType {
	case "/play":
		var tk = q.Get("tk")
		userID, ok := token.ParseToken(tk)
		if !ok {
			cl.Printf("invalid token, Peer: %s", r.RemoteAddr)
			return
		}

		if config.RequiredAppModuleVer > 0 {
			appModuleVer, err := strconv.Atoi(q.Get("amv"))
			if err != nil || appModuleVer < config.RequiredAppModuleVer {
				cl.Printf("app module too old, ID:%s, Peer:%s\n", userID, r.RemoteAddr)
				tables.SendEnterTableResult(cl, ws, userID, xproto.EnterTableStatus_AppModuleNeedUpgrade)
				return
			}
		}

		// table uuid
		var tableUID = q.Get("tuid")
		acceptPlayer(userID, tableUID, ws, r)
		break
	case "/monkey":
		var tableIDString = q.Get("tuid")
		if tableIDString == "" {
			var tableNumber = q.Get("tnid")
			if tableNumber == "" {
				cl.Println("monkey has no table uuid and table number id")
				return
			}

			table := tables.GetMgr().GetTableByNumber(tableNumber)
			if table != nil {
				tableIDString = table.UUID
			} else {
				cl.Println("no talbe found for table number:", tableNumber)
				return
			}
		}

		var userID = q.Get("userID")
		acceptPlayer(userID, tableIDString, ws, r)
		break
	}
}

func acceptPlayer(userID string, tableIDString string, ws *websocket.Conn, r *http.Request) {
	// found target table
	table := tables.GetMgr().GetTable(tableIDString)
	if table == nil {
		log.Printf("can't found table with ID:%s, Peer:%s\n", tableIDString, r.RemoteAddr)
		// TODO: send error to client
		// sendEnterRoomError(ws, userID, pokerface.EnterRoomStatus_RoomNotExist)
		return
	}

	var player *tables.Player
	table.HoldLock(func() {
		player = table.OnPlayerEnter(ws, userID)
	})

	if player != nil {
		drainPlayerWebsocket(player, ws)
	}
}

func drainPlayerWebsocket(player *tables.Player, ws *websocket.Conn) {
	ws.SetPongHandler(func(msg string) error {
		player.OnPong(ws, msg)
		return nil
	})

	ws.SetPingHandler(func(msg string) error {
		player.OnPing(ws, msg)
		return nil
	})

	var errExit error
	// ensure to notify player that we exit websocket reading anyway
	defer player.OnExitMsgLoop(ws, errExit)

	for {
		mt, message, err := ws.ReadMessage()
		if err != nil {
			log.Printf("player %s websocket receive error:%v", player.ID, err)
			errExit = err
			ws.Close()
			break
		}

		if message != nil && len(message) > 0 && mt == websocket.BinaryMessage {
			player.OnWebsocketMessage(ws, message)
		}
	}
}
