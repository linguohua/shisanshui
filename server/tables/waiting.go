package tables

import (
	"runtime/debug"
	"shisanshui/xproto"
	"time"

	"github.com/sirupsen/logrus"
)

// stateWaiting table waiting state
// 牌桌由初始的idle状态转入waiting状态，等待游戏开始
type stateWaiting struct {
	table *Table
	cl    *logrus.Entry

	countdownTick       int
	inCountingDownState bool
}

func waitingStateNew(t *Table) *stateWaiting {
	return &stateWaiting{
		table: t,
		cl:    t.cl.WithField("src", "waiting state"),
	}
}

func (s *stateWaiting) name() string {
	return "waiting"
}

func (s *stateWaiting) onPlayerEnter(p *Player) {
	p.state = xproto.PlayerState_PSNone
}

func (s *stateWaiting) onPlayerReConnect(p *Player) {
	// restore
	p.state = xproto.PlayerState_PSNone
	s.table.updateTableInfo2All(int32(s.countdownTick))
}

func (s *stateWaiting) onPlayerOffline(p *Player) {
	p.state = xproto.PlayerState_PSOffline
	s.table.stateRemovePlayer(p)
	s.table.updateTableInfo2All(int32(s.countdownTick))
}

func (s *stateWaiting) onPlayerMsg(p *Player, msg *xproto.GameMessage) {
	// 等待状态下，只处理用户ready消息，当所有人都Ready后
	// 进入playing状态
	code := msg.GetCode()
	switch code {
	case int32(xproto.MessageCode_OPPlayerReady):
		s.cl.Printf("got player ready:%d, total player count:%d", p.chairID, len(s.table.players))
		if p.state == xproto.PlayerState_PSReady {
			// 重复收到ready消息
			break
		}

		p.state = xproto.PlayerState_PSReady

		s.tryCountingDown()

		// 有用户状态发生了变化，更新给所有客户端
		s.table.updateTableInfo2All(int32(s.countdownTick))

		break

	default:
		s.cl.Println("Waiting state can not process msg:", code)
		break
	}
}

func (s *stateWaiting) onStateEnter() {
	s.cl.Println("onStateEnter")

	s.tryCountingDown()
	if s.inCountingDownState {
		s.table.updateTableInfo2All(int32(s.countdownTick))
	}
}

func (s *stateWaiting) onStateExit() {
	s.cl.Println("onStateExit")
}

func (s *stateWaiting) getStateConst() xproto.TableState {
	return xproto.TableState_STableWaiting
}

func (s *stateWaiting) tryCountingDown() {
	if s.inCountingDownState {
		s.cl.Println("tryCountingDown: already in countdown")
		return
	}

	readyCount := 0
	for _, p := range s.table.players {
		s.cl.Printf("tryCountingDown: player chairID:%d, state:%v", p.chairID, p.state)
		if p.state == xproto.PlayerState_PSReady {
			readyCount++
		}
	}

	s.cl.Printf("tryCountingDown: current ready player:%d, required:%d", readyCount, s.table.config.PlayerNumAcquired)
	if readyCount >= s.table.config.PlayerNumAcquired {
		// start counting down, startCountingDown can call multiple times
		s.doCountingDown()
	}
}

func (s *stateWaiting) doCountingDown() {
	s.countdownTick = s.table.config.Countdown
	s.cl.Printf("table start to countdown with %d seconds", s.countdownTick)
	s.inCountingDownState = true

	go s.countdownGoroutine()
}

func (s *stateWaiting) countdownGoroutine() {
	defer func() {
		if r := recover(); r != nil {
			debug.PrintStack()
			s.cl.Printf("-----PANIC: This Table will die, STACK\n:%v", r)
			mgr.IncExceptionCount()
		}
	}()

	loop := true
	for loop {

		time.Sleep(time.Second)
		// new goroutine call into here, so onCountdownCompleted
		// must be concurrent safe
		s.table.HoldLock(func() {
			loop = s.onCountdownStep()
		})
	}
}

func (s *stateWaiting) onCountdownStep() bool {
	if s.countdownTick > 0 {
		s.countdownTick--
	}

	if s.countdownTick == 0 {
		playerCount := len(s.table.players)
		required := s.table.config.PlayerNumAcquired
		if playerCount < required {
			s.cl.Printf("current player count %d < required(%d), continue waitig",
				playerCount, required)

			s.inCountingDownState = false

		} else {
			s.table.stateTo(playingStateNew(s.table))
		}

		return false
	}

	return true
}
