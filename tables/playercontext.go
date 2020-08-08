package tables

// phandContext context for one hand card
// reset when one hand completed
// 每一手牌开始时重新生成
type phandContext struct {
	expectedAction int
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
