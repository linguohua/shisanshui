package tables

import (
	"github.com/sirupsen/logrus"
)

// playingContext 游戏进行时的上下文
// 主要作用是保存游戏的行牌过程记录，例如发牌记录等等，可以作回播用，
// 也可提供玩家行牌过程中的动作备忘，例如某个玩家弃牌次数等
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
