package tables

import "github.com/sirupsen/logrus"

// actionWaiter 等待玩家响应
type actionWaiter struct {
	cl *logrus.Entry
	s  *statePlaying

	isFinished bool
	chanWait   chan bool
}

func actionWaiterNew(s *statePlaying) *actionWaiter {
	aw := &actionWaiter{
		cl: s.cl.WithField("src", "action waiter"),
		s:  s,
	}

	// TODO: construct players list

	aw.chanWait = make(chan bool, 1) // buffered channel,1 slots

	return aw
}

func (aw *actionWaiter) wait() bool {
	if aw.isFinished {
		return false
	}

	// release table's lock, thus other goroutine can enter
	// table's function
	aw.s.table.lock.Unlock()
	result := <-aw.chanWait
	// re-lock table,to continue gameloop workflow
	aw.s.table.lock.Lock()

	if result == false {
		return result
	}

	return result
}
