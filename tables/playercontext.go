package tables

// phandContext context for one hand card
// reset when one hand completed
type phandContext struct {
	expectedAction int
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
