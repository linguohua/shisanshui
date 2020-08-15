package tables

import (
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

// stateWaiting table waiting state
// 牌桌由初始的idle状态转入waiting状态，等待游戏开始
type stateWaiting struct {
	table *Table
	cl    *logrus.Entry
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
	s.table.updateTableInfo2All()

	playerCount := len(s.table.players)
	if playerCount >= s.table.config.PlayerNumAcquired {
		if !s.table.countingDown {
			s.table.startCountingDown()
		}
	}
}

func (s *stateWaiting) onPlayerReConnect(p *Player) {
	// restore
	p.state = xproto.PlayerState_PSNone
	s.table.updateTableInfo2All()
}

func (s *stateWaiting) onPlayerOffline(p *Player) {
	p.state = xproto.PlayerState_PSOffline
	s.table.updateTableInfo2All()
}

func (s *stateWaiting) onPlayerMsg(p *Player, msg *xproto.GameMessage) {
	s.cl.Panic("waiting state should not proc onPlayerMsg")
}

func (s *stateWaiting) onStateEnter() {
	s.cl.Println("onStateEnter")
}

func (s *stateWaiting) onStateExit() {
	s.cl.Println("onStateExit")
}

func (s *stateWaiting) getStateConst() xproto.TableState {
	return xproto.TableState_STableWaiting
}
