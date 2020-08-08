package tables

import (
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

// stateIdle table idle state
type stateIdle struct {
	table *Table
	cl    *logrus.Entry
}

func idleStateNew(t *Table) *stateIdle {
	return &stateIdle{
		table: t,
		cl:    t.cl.WithField("src", "idle state"),
	}
}

func (s *stateIdle) name() string {
	return "idle"
}

func (s *stateIdle) onPlayerEnter(p *Player) {
	s.table.stateTo(waitingStateNew(s.table))
	s.table.state.onPlayerEnter(p)
}

func (s *stateIdle) onPlayerReConnect(p *Player) {
	s.cl.Panic("idle state should not proc onPlayerReConnect")
}

func (s *stateIdle) onPlayerOffline(p *Player) {
	s.cl.Panic("idle state should not proc onPlayerOffline")
}

func (s *stateIdle) onPlayerMsg(p *Player, msg *xproto.GameMessage) {
	s.cl.Panic("idle state should not proc onPlayerMsg")
}

func (s *stateIdle) onStateEnter() {
	s.cl.Println("onStateEnter")
}

func (s *stateIdle) onStateExit() {
	s.cl.Println("onStateExit")
}
