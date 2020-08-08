package tables

import (
	"github.com/sirupsen/logrus"
)

type playingContext struct {
	cl *logrus.Entry

	state *statePlaying
}

func playingContextNew(s *statePlaying) *playingContext {
	cl := s.cl.WithField("src", "playingcontext")
	return &playingContext{
		cl:    cl,
		state: s,
	}
}

func (pc *playingContext) snapshootDealActions() {
	// TODO:
}

func (pc *playingContext) dump2Redis() {
	// TODO:
}

func (pc *playingContext) isCompleted() bool {
	// TODO:
	return true
}
