package tables

import (
	"runtime/debug"
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
	"google.golang.org/protobuf/proto"
)

// stateIdle table idle state
// 牌桌游戏进行中，此时新玩家进入应该作为旁观者，等待下一手开始才作为参与者加入
// 玩家重连进入时，需要进行恢复流程，也就是需要把玩家的手牌等数据重新发给客户端，以便他能继续游戏
type statePlaying struct {
	table *Table
	cl    *logrus.Entry

	playingPlayers  []*Player
	watchingPlayers []*Player

	dealer     *dealer
	playingCtx *playingContext

	waiter *actionWaiter
}

func playingStateNew(t *Table) *statePlaying {
	s := &statePlaying{
		table: t,
		cl:    t.cl.WithField("src", "playing state"),
	}

	s.playingPlayers = make([]*Player, len(t.players))
	for i, p := range t.players {
		s.playingPlayers[i] = p
	}
	s.watchingPlayers = make([]*Player, 0)

	s.dealer = dealerNew(t, s.playingPlayers)
	return s
}

func (s *statePlaying) name() string {
	return "playing"
}

func (s *statePlaying) onPlayerEnter(p *Player) {
	// add to watching player list
	p.state = xproto.PlayerState_PSNone
	s.watchingPlayers = append(s.watchingPlayers, p)
	s.table.updateTableInfo2All(0)
}

func (s *statePlaying) onPlayerReConnect(p *Player) {
	// restore
	p.state = xproto.PlayerState_PSPlaying
	s.table.updateTableInfo2All(0)
	msgRestore := serializeMsgRestore(s, p)
	p.sendGameMsg(msgRestore, int32(xproto.MessageCode_OPRestore))
}

func (s *statePlaying) onPlayerOffline(p *Player) {
	p.state = xproto.PlayerState_PSOffline
	s.table.updateTableInfo2All(0)
}

func (s *statePlaying) onPlayerMsg(p *Player, gmsg *xproto.GameMessage) {
	var msgCode = xproto.MessageCode(gmsg.GetCode())

	switch msgCode {
	case xproto.MessageCode_OPAction:
		actionMsg := &xproto.MsgPlayerAction{}
		err := proto.Unmarshal(gmsg.GetData(), actionMsg)
		if err == nil {
			s.onActionMessage(p, actionMsg)
		} else {
			s.cl.Println("onPlayerMsg unmarshal error:", err)
		}
		break
	default:
		s.cl.Println("onPlayerMsg unsupported msgCode:", msgCode)
		break
	}
}

func (s *statePlaying) onActionMessage(p *Player, msg *xproto.MsgPlayerAction) {
	if s.table.qaIndex != (int(msg.GetQaIndex())) {
		s.cl.Printf("OnMessageAction error, qaIndex %d not expected, use id:%s, name:%s", msg.GetQaIndex(), p.ID, p.Name)
		return
	}

	if p.rContext.expectedAction&int(msg.GetAction()) == 0 {
		s.cl.Printf("OnMessageAction allow actions %d not match %d, userId:%s, name:%s",
			p.rContext.expectedAction, msg.GetAction(), p.ID, p.Name)
		s.cl.Panic("action not expected")
		return
	}

	// reset player expected actions
	p.rContext.expectedAction = 0

	var action = xproto.ActionType(msg.GetAction())
	switch action {
	case xproto.ActionType_enumActionType_DISCARD:
		onMessageDiscardHandler(s, p, msg)
		break

	default:
		s.cl.Panic("OnMessageAction unsupported action code")
		break
	}
}

func (s *statePlaying) onStateEnter() {
	s.cl.Println("onStateEnter")

	for _, p := range s.playingPlayers {
		p.rContext = &roundContext{}
		p.cards = cardlistNew(p)
	}

	// 进入游戏循环
	go s.table.HoldLock(s.gameLoop)
}

func (s *statePlaying) onStateExit() {
	s.cl.Println("onStateExit")
}

func (s *statePlaying) gameLoop() {
	s.cl.Println("gameloop begin")
	// 如果gameLoop中的goroutine出错，则该房间挂死，但是不影响其他房间
	defer func() {
		if r := recover(); r != nil {
			debug.PrintStack()
			s.cl.Printf("-----PANIC: This Table will die, STACK\n:%v", r)
			mgr.IncExceptionCount()
		}
	}()

	s.playingCtx = playingContextNew(s)

	// 发牌
	s.dealer.drawForAll()

	// 给所有客户端发牌
	for _, player := range s.playingPlayers {
		var msgDeal = serializeMsgDeal(s, player)
		player.sendGameMsg(msgDeal, int32(xproto.MessageCode_OPDeal))
	}

	// 保存发牌数据
	s.playingCtx.snapshootDealActions()

	for {
		if !s.waitPlayersAction() {
			s.cl.Panic("waitPlayersAction should not return false")
		}

		s.handOver()

		break
	}

	if !s.table.isForMonkey && s.playingCtx.isCompleted() {
		s.playingCtx.dump2Redis()
	}

	s.playingCtx = nil

	s.cl.Println("gameloop end")

	// state change to waiting
	s.table.stateTo(waitingStateNew(s.table))
}

func (s *statePlaying) waitPlayersAction() bool {
	// send request to all playing players
	var qaIndex = s.table.nextQAIndex()
	// 填写客户端可以操作的动作
	actions := int(xproto.ActionType_enumActionType_DISCARD)

	for _, p := range s.playingPlayers {
		p.rContext.expectedAction = actions
		//TODO : 10要写到配置里(理牌时间)
		msgAllowPlayerAction := serializeMsgAllowedForDiscard(s, p, actions, qaIndex, 10)
		p.sendGameMsg(msgAllowPlayerAction, int32(xproto.MessageCode_OPActionAllowed))

		if s.table.isForceConsistent() {
			s.sendMonkeyTips(p)
		}
	}

	// wait playing players reply
	s.waiter = actionWaiterNew(s)
	return s.waiter.wait()
}

func (s *statePlaying) handOver() {
	//计算结果
	compareAndCalcScore(s)

	msgHandOver := serializeMsgHandOver(s)
	s.table.onHandOver(msgHandOver)
}

func (s *statePlaying) sendMonkeyTips(p *Player) {
	// TODO:
}

func (s *statePlaying) getStateConst() xproto.TableState {
	return xproto.TableState_STablePlaying
}
