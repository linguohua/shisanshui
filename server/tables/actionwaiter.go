package tables

import (
	"container/list"

	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

// actionWaiter 等待玩家响应
type actionWaiter struct {
	cl *logrus.Entry
	s  *statePlaying

	isFinished bool
	chanWait   chan bool

	waitQueue *list.List
}

// TaskExchangeQueueItem 等待队列项
type TaskExchangeQueueItem struct {
	player     *Player
	reply      bool
	waitAction int
	cardsList  []int32
}

func actionWaiterNew(s *statePlaying) *actionWaiter {
	//此游戏只有这一个等待任务 所以waitAction只会是DISCARD
	aw := &actionWaiter{
		cl: s.cl.WithField("src", "action waiter"),
		s:  s,
	}

	// TODO: construct players list
	aw.waitQueue = list.New()
	for _, p := range s.playingPlayers {
		ti := &TaskExchangeQueueItem{}
		ti.player = p
		ti.waitAction = int(xproto.ActionType_enumActionType_DISCARD)

		aw.waitQueue.PushBack(ti)
	}

	aw.chanWait = make(chan bool, 1) // buffered channel,1 slots

	return aw
}

func (aw *actionWaiter) wait() bool {
	if aw.isFinished {
		return false
	}

	// release table's lock, thus other goroutine can enter
	// table's function
	var result bool
	aw.s.table.yieldLock(func() {
		result = <-aw.chanWait
	})

	if result == false {
		return result
	}

	return result
}

// findWaitQueueItem 根据player找到wait item
func (aw *actionWaiter) findWaitQueueItem(player *Player) *TaskExchangeQueueItem {
	for e := aw.waitQueue.Front(); e != nil; e = e.Next() {
		qi := e.Value.(*TaskExchangeQueueItem)
		if qi.player == player {
			return qi
		}
	}
	return nil
}

// takeAction 玩家做了选择
func (aw *actionWaiter) takeAction(player *Player, action int, cardIDs []int32) {

	wi := aw.findWaitQueueItem(player)
	if wi == nil {
		aw.cl.Printf("player %s not in TaskExchange queue", player.ID)
		return
	}

	if wi.reply {
		aw.cl.Printf("player %s ha already reply TaskExchange queue", player.ID)
		return
	}

	wi.reply = true
	wi.cardsList = cardIDs

	//保存排序好的牌到player
	//wi.player.hcontext.sortCards = cardIDs

	allReply := true
	for e := aw.waitQueue.Front(); e != nil; e = e.Next() {
		qi := e.Value.(*TaskExchangeQueueItem)
		if !qi.reply {
			allReply = false
			break
		}
	}

	if allReply {
		aw.completed(true)
	}
}

// completed 完成等待
func (aw *actionWaiter) completed(result bool) {
	if aw.isFinished {
		return
	}

	aw.isFinished = true
	if aw.chanWait == nil {
		return
	}

	aw.chanWait <- result
}

func (aw *actionWaiter) cancel() {
	aw.completed(false)
}
