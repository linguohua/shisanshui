package tables

import (
	"sync"
	"time"

	"github.com/gorilla/websocket"
	"github.com/sirupsen/logrus"
	"google.golang.org/protobuf/proto"
)

const (
	websocketWriteDeadLine = 3 * time.Second
)

// Player game player
// 玩家在牌桌服务器的实例，包含玩家客户端的webocket通讯连接对象，以及玩家所在的牌桌
// 玩家的个人基本信息，游戏进行过程中需要的数据（例如牌列表，得分等等）
type Player struct {
	// context logger, print with player uuid, player name, table uuid, etc...
	cl *logrus.Entry

	// player uuid
	ID string
	// player name
	Name string

	// our table
	table   *Table
	chairID int

	cards *cardlist

	hcontext *phandContext
	gcontext *pgameContext

	// websocket connection
	ws *websocket.Conn
	// websocket concurrent write lock
	wsWriteLock *sync.Mutex

	lastMsgTime time.Time
}

func newPlayer(t *Table, chairID int, ws *websocket.Conn, userID string) *Player {
	p := &Player{
		ID:      userID,
		table:   t,
		chairID: chairID,
		ws:      ws,
	}

	p.pullinfo()

	fields := make(logrus.Fields)
	fields["player useID"] = userID
	fields["player name"] = p.Name
	fields["src"] = "player"

	p.cl = t.cl.WithFields(fields)

	return p
}

// send write buffer to player's websocket connection
func (p *Player) send(bytes []byte) {
	ws := p.ws
	if ws != nil {
		p.wsWriteLock.Lock()
		defer p.wsWriteLock.Unlock()

		// write deadline for write timeout
		ws.SetWriteDeadline(time.Now().Add(websocketWriteDeadLine))
		err := ws.WriteMessage(websocket.BinaryMessage, bytes)
		if err != nil {
			ws.Close()
			p.cl.Println("web socket write err:", err)
		}
	}
}

func (p *Player) sendGameMsg(pb proto.Message, code int32) {
	bytes, err := formatGameMsg(p.cl, pb, code)
	if err != nil {
		p.cl.Panic("marshal game msg failed:", err)
	}

	p.send(bytes)
}

// OnPong handle pong message
func (p *Player) OnPong(ws *websocket.Conn, msg string) {
	if p.ws != ws {
		return
	}

	p.lastMsgTime = time.Now()

	// TODO: calc RTT and store
}

// OnPing handle ping message
func (p *Player) OnPing(ws *websocket.Conn, msg string) {
	if p.ws != ws {
		return
	}

	p.send([]byte(msg))

	p.lastMsgTime = time.Now()
}

// rebind rebind websocket instance
func (p *Player) rebind(ws *websocket.Conn) {
	if p.ws == ws {
		p.cl.Warn("player rebind websocket failed: ws same instance")
		return
	}

	p.unbind()
	p.ws = ws
}

// unbind close websocket instance
func (p *Player) unbind() {
	old := p.ws
	p.ws = nil

	if old != nil {
		// close the old instance
		old.Close()
	}
}

// OnExitMsgLoop handle websocket reading loop, indicates that
// the websocket connection has been closed or internal error ocurrs
func (p *Player) OnExitMsgLoop(ws1 *websocket.Conn, err error) {
	p.cl.Println("OnExitMsgLoop:", err)

	if p.ws != ws1 {
		return
	}

	ws := p.ws
	if ws != nil {
		ws.Close()
	}

	table := p.table
	if table != nil {
		table.onPlayerOffline(p)
	}
}

// OnWebsocketMessage handle player's network message
func (p *Player) OnWebsocketMessage(ws *websocket.Conn, msg []byte) {
	if p.ws != ws {
		return
	}

	p.lastMsgTime = time.Now()

	table := p.table
	if table != nil {
		table.OnPlayerMsg(p, msg)
	}
}

// pullinfo load personal info
func (p *Player) pullinfo() {
	// TODO: pull info fram redis
}
