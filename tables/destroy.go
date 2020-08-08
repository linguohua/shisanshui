package tables

import (
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

// stateDestroy table idle state
type stateDestroy struct {
	table *Table
	cl    *logrus.Entry
}

func destroyStateNew(t *Table) *stateDestroy {
	return &stateDestroy{
		table: t,
		cl:    t.cl.WithField("src", "destroy state"),
	}
}

func (s *stateDestroy) name() string {
	return "destroy"
}

func (s *stateDestroy) onPlayerEnter(p *Player) {

}

func (s *stateDestroy) onPlayerReConnect(p *Player) {

}

func (s *stateDestroy) onPlayerOffline(p *Player) {

}

func (s *stateDestroy) onPlayerMsg(p *Player, msg *xproto.GameMessage) {

}

func (s *stateDestroy) onStateEnter() {
	s.cl.Println("onStateEnter")
}

func (s *stateDestroy) onStateExit() {
	s.cl.Println("onStateExit")
}
