// 一些辅助函数

package tables

import (
	"math/rand"

	"github.com/gorilla/websocket"
)

// byChairID 根据座位ID排序
type byChairID []*Player

func (s byChairID) Len() int {
	return len(s)
}

func (s byChairID) Swap(i, j int) {
	s[i], s[j] = s[j], s[i]
}

func (s byChairID) Less(i, j int) bool {
	return s[i].chairID < s[j].chairID
}

// byChairIDIndex 根据座位ID排序
type byChairIDIndex struct {
	chairIDs       []int
	chairIDIndexes []int
}

func (s *byChairIDIndex) Len() int {
	return len(s.chairIDs)
}

func (s *byChairIDIndex) Swap(i, j int) {
	s.chairIDs[i], s.chairIDs[j] = s.chairIDs[j], s.chairIDs[i]
}

func (s *byChairIDIndex) Less(i, j int) bool {
	return s.chairIDIndexes[s.chairIDs[i]] < s.chairIDIndexes[s.chairIDs[j]]
}

// Implementing Fisher–Yates shuffle
func shuffleArray(ar []*card, rnd *rand.Rand) []*card {
	// If running on Java 6 or older, use `new Random()` on RHS here
	for i := len(ar) - 1; i > 0; i-- {
		index := rnd.Intn(i + 1)
		// Simple swap
		a := ar[index]
		ar[index] = ar[i]
		ar[i] = a
	}

	return ar
}

// SendEnterTableResult send error code to client
// public, can be called out of tables package
func SendEnterTableResult(ws *websocket.Conn, userID string, errorCode int32) {

}
