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
	playerCount := len(s.table.players)
	if playerCount >= s.table.config.playerNumAcquired {
		if !s.table.countingDown {
			s.table.startCountingDown()
		}
	}
}

func (s *stateWaiting) onPlayerReConnect(p *Player) {

}

func (s *stateWaiting) onPlayerOffline(p *Player) {

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
