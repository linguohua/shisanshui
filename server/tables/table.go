package tables

import (
	"fmt"
	"math/rand"
	"shisanshui/xproto"
	"sort"
	"sync"
	"time"

	"github.com/gorilla/websocket"
	"github.com/sirupsen/logrus"
	"google.golang.org/protobuf/proto"
)

var (
	chairAllocOrder = []int{0, 2, 1, 3}
)

// state table state
type state interface {
	name() string

	onPlayerEnter(p *Player)
	onPlayerReConnect(p *Player)
	onPlayerOffline(p *Player)
	onPlayerMsg(p *Player, msg *xproto.GameMessage)

	getStateConst() xproto.TableState

	onStateEnter()
	onStateExit()
}

// Table hold players and rule the game
// 牌桌是游戏服务器进程管理的最基本单元，所有玩家都必须归属于某一个牌桌，
// 一个玩家不能在两个牌桌上。每一个table都有一个lock，用来保证多个goroutine
// 操作table时的数据完整，因此，所有goroutine访问table，或者table所拥有的players
// 时，都需要先持有这个lock，否则没法保证其他的goroutine是否并发访问这些数据。
// 目前已知的需要访问table和其player的goroutine有：
//     1. 每一个玩家的websocket接收goroutine，例如牌桌4个玩家，就有4个这样的goroutine
//     2. table进入playing状态时，playing状态下有一个gameloop goroutine
//     3. waiting状态下，启动的倒计时定时器的goroutine
// 牌桌由大厅服务器下发创建牌桌的消息给游戏服务器，由后者创建。销毁目前也是大厅服务器下发指令销毁。
type Table struct {
	// lock when goroutine operate on table
	lock sync.Mutex
	// context logger, print with table uuid, table nid, etc...
	cl *logrus.Entry

	bankerChairID int

	// table uuid
	UUID   string
	Number string

	state state

	// char index
	chairs           []int
	chairSortIndexes []int
	// players
	players []*Player

	qaIndex int
	rand    *rand.Rand

	config *tableConfig

	monkeyCfg *monkeyConfig

	lastMsgTime time.Time

	isForMonkey bool
}

// tableNew new a table
func tableNew(uuid string, number string, cfg *tableConfig) *Table {
	fields := make(logrus.Fields)
	fields["src"] = "table"
	fields["table-uuid"] = uuid
	fields["table-number"] = number

	cl := logrus.WithFields(fields)

	t := &Table{
		cl:      cl,
		UUID:    uuid,
		Number:  number,
		rand:    rand.New(rand.NewSource(time.Now().UnixNano())),
		config:  cfg,
		players: make([]*Player, 0),
	}

	t.chairs = append(t.chairs, chairAllocOrder...)
	t.chairSortIndexes = append(t.chairSortIndexes, chairAllocOrder...)

	// init to idle state
	t.state = idleStateNew(t)

	return t
}

func newNewForMonkey(uuid string, number string, cfg *tableConfig) *Table {
	t := tableNew(uuid, number, cfg)

	t.isForMonkey = true
	return t
}

func (t *Table) initChair() {

	t.chairs = make([]int, 0, 4)
	t.chairs = append(t.chairs, chairAllocOrder...)
	t.chairSortIndexes = make([]int, 0, 4)
	for i, v := range t.chairs {
		t.chairSortIndexes[v] = i
	}
}

// OnPlayerEnter handle player enter table event
// Note: concurrent safe
func (t *Table) OnPlayerEnter(ws *websocket.Conn, userID string) *Player {
	t.cl.Printf("OnPlayerEnter table, userID:%s", userID)

	player := t.getPlayerByUserID(userID)

	// 如果房间是monkey房间，且其配置要求强制一致，则userID必须位于配置中
	if t.isForceConsistent() && player == nil {
		monkeyUserCardCfg := t.monkeyCfg.getMonkeyCardCfg(userID)
		if monkeyUserCardCfg == nil {
			SendEnterTableResult(t.cl, ws, userID, xproto.EnterTableStatus_MonkeyTableUserIDNotMatch)
			return nil
		}

		// 而且玩家进入的顺序必须严格按照配置指定
		loginSeq := len(t.players)
		if loginSeq != monkeyUserCardCfg.index {
			SendEnterTableResult(t.cl, ws, userID, xproto.EnterTableStatus_MonkeyTableUserLoginSeqNotMatch)
			return nil
		}
	}

	if player != nil {
		// 用户存在，则可能是如下原因：
		// 		客户端代码判断自己已经离线，然后重连服务器
		//		服务器也知道客户端已经离线并在等客户端上线
		return t.onPlayerReconnect(player, ws)
	}

	if len(t.players) == t.config.PlayerNumMax {
		// 已经满员
		SendEnterTableResult(t.cl, ws, userID, xproto.EnterTableStatus_TableIsFulled)
		return nil
	}

	// 可以进入房间，新建player对象x
	chairID := t.allocChair(-1)
	player = newPlayer(t, chairID, ws, userID)

	// 增加到玩家列表
	t.players = append(t.players, player)
	// 根据座位ID排序
	sort.Sort(byChairID(t.players))

	// 发送成功进入房间通知给客户端
	SendEnterTableResult(t.cl, ws, userID, xproto.EnterTableStatus_Success)

	// state handle
	t.state.onPlayerEnter(player)

	// 写redis数据库，以便其他服务器能够知道玩家进入该房间
	t.writePlayerEnterEvent2Redis(player)

	return player
}

// onPlayerReconnect handle player re-connect flow
func (t *Table) onPlayerReconnect(p *Player, ws *websocket.Conn) *Player {
	t.cl.Printf("onPlayerReconnect, userID:%s, table number:%s", p.ID, t.Number)

	// 更新用户信息
	p.pullinfo()

	// 更换websocket连接
	p.rebind(ws)

	// 发送成功进入房间通知给客户端
	SendEnterTableResult(t.cl, ws, p.ID, xproto.EnterTableStatus_Success)

	// 通知状态机
	t.state.onPlayerReConnect(p)

	// 写redis数据库，以便其他服务器能够知道玩家进入该房间
	t.writePlayerEnterEvent2Redis(p)

	return p
}

// onPlayerOffline 处理用户离线，不同的状态下，玩家离线表现不同
// 例如，如果是等待状态，且游戏并没有开始，那么玩家离线后，其player对象会被清除
// 但是如果是游戏正在进行，那么玩家离线，其player对象不会被清除，而一直等待其上线
// 或者直到其他玩家决定解散本局游戏
func (t *Table) onPlayerOffline(player *Player) {
	// 让状态机处理用户离线
	// 不同状态下对用户离线的处理是不同的，比如Waiting状态，用户离线会把Player删除
	// 也即是Waiting状态下用户随意进出。但在Playing状态下，用户离线Player对象一直保留
	// 除非其他玩家选择解散本局游戏
	t.state.onPlayerOffline(player)
}

// onPlayerMsg handle player network message
// Note: concurrent safe
func (t *Table) onPlayerMsg(player *Player, msg []byte) {
	gmsg := &xproto.GameMessage{}
	err := proto.Unmarshal(msg, gmsg)
	if err != nil {
		t.cl.Println("onUserMessage, unmarshal error:", err)
		return
	}

	// 记录一下最后一个消息的接收时间
	t.lastMsgTime = time.Now()

	var msgCode = gmsg.GetCode()
	var handled = false
	switch msgCode {
	case int32(xproto.MessageCode_OPKickout):
		break
	default:
		break
	}

	if handled {
		return
	}

	// 不是房间可以处理的消息，交给状态机
	t.state.onPlayerMsg(player, gmsg)
}

func (t *Table) stateTo(newState state) {
	if newState.name() == t.state.name() {
		t.cl.Panic("stateTo same type state")
	}

	oldState := t.state
	oldState.onStateExit()

	t.state = newState
	t.state.onStateEnter()
}

// getPlayerByUserID 根据userID获取player对象
func (t *Table) getPlayerByUserID(userID string) *Player {
	for _, p := range t.players {
		if p.ID == userID {
			return p
		}
	}

	return nil
}

// getPlayerByChairID 根据ChairID获取player对象
func (t *Table) getPlayerByChairID(chairID int) *Player {
	for _, p := range t.players {
		if p.chairID == chairID {
			return p
		}
	}

	return nil
}

func (t *Table) isForceConsistent() bool {
	return false
}

// allocChair 申请一个座位, fixChairID如果大于-1表示指定了座位
func (t *Table) allocChair(fixChairID int) int {
	if len(t.chairs) == 0 {
		t.cl.Panic("no chair id to alloc")
		return -1
	}

	var result = -1
	if fixChairID >= 0 {
		for i, c := range t.chairs {
			if c == fixChairID {
				result = fixChairID
				t.chairs = append(t.chairs[0:i], t.chairs[i+1:]...)
			}
		}
	}

	if result < 0 {
		result = t.chairs[0]
		t.chairs = t.chairs[1:]
	}

	return result
}

// releaseChair 归还一个座位
func (t *Table) releaseChair(chairID int) {
	if len(t.chairs) == t.config.PlayerNumMax {
		t.cl.Panic("releaseChair failed: chair array is fulled")
		return
	}

	t.chairs = append(t.chairs, chairID)
	// 排序座位
	sort.Sort(&byChairIDIndex{chairIDs: t.chairs, chairIDIndexes: t.chairSortIndexes})
}

func (t *Table) writePlayerEnterEvent2Redis(player *Player) {
	// TODO: write event 2 redis
}

func (t *Table) yieldLock(func1 func()) {
	t.lock.Unlock()
	defer t.lock.Lock() // re-lock

	func1()
}

// HoldLock hold table lock and execute func
func (t *Table) HoldLock(func1 func()) {
	t.lock.Lock()
	defer t.lock.Unlock()

	func1()
}

func (t *Table) nextQAIndex() int {
	t.qaIndex++
	return t.qaIndex
}

func (t *Table) destroy(reason xproto.TableDeleteReason) {
	// 通知所有人房间已经被删除
	msgDelete := &xproto.MsgTableDelete{}
	var reason32 = int32(reason)
	msgDelete.Reason = &reason32
	for _, p := range t.players {
		p.sendGameMsg(msgDelete, int32(xproto.MessageCode_OPTableDeleted))
	}
	// 强制停止游戏，状态转换到deleted状态
	t.stateTo(destroyStateNew(t))

	// 断开玩家的链接
	t.kickAll()

	t.cl.Println("table destroyed")
}

func (t *Table) kickAll() {
	// 断开玩家的链接
	for _, p := range t.players {
		p.ws.Close()
	}
}

func (t *Table) kickPlayer(userID string) error {
	//TODO 通知客户端
	p := t.getPlayerByUserID(userID)
	if p != nil {
		p.ws.Close()
	}

	return fmt.Errorf("not implement")
}

// stateRemovePlayer 用于状态机从玩家列表中移除玩家
// 玩家移除后，需要归还座位
func (t *Table) stateRemovePlayer(player *Player) {
	for i, p := range t.players {
		if p == player {
			t.players = append(t.players[0:i], t.players[i+1:]...)
			// 归还座位
			t.releaseChair(p.chairID)
			break
		}
	}
}

// updateTableInfo2All 把房间当前状态和玩家数据发给所有用户
func (t *Table) updateTableInfo2All(timeout int32) {
	if len(t.players) > 0 {
		var msgTableInfo = serializeMsgTableInfo(t, timeout)
		for _, p := range t.players {
			p.sendGameMsg(msgTableInfo, int32(xproto.MessageCode_OPTableUpdate))
		}
	}

	// r.pushState2TableMgrServer()
}

// onHandOver 一手牌结局
func (t *Table) onHandOver(msgHandOver *xproto.MsgHandOver) {

	// 重置qaIndex
	t.qaIndex = 0

	// 下一手牌，所以直接进入等待状态而不是空闲状态
	t.stateTo(waitingStateNew(t))

	for _, p := range t.players {
		// p.state = xproto.PlayerState_PSNone

		// 确保状态已经切换到SWaiting后，才发送手牌结果给客户端
		p.sendGameMsg(msgHandOver, int32(xproto.MessageCode_OPHandOver))
	}
}
