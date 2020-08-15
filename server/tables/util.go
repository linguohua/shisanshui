// 一些辅助函数

package tables

import (
	"math/rand"
	"shisanshui/xproto"

	"github.com/gorilla/websocket"
	log "github.com/sirupsen/logrus"
	"google.golang.org/protobuf/proto"
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
func SendEnterTableResult(cl *log.Entry, ws *websocket.Conn, userID string, errorCode xproto.EnterTableStatus) {
	if errorCode != xproto.EnterTableStatus_Success {
		statusStr, ok := xproto.EnterTableStatus_name[int32(errorCode)]
		if ok {
			cl.Printf("SendEnterTableResult, userID:%s, error:%s\n", userID, statusStr)
		} else {
			cl.Printf("SendEnterTableResult, userID:%s, error:%d\n", userID, errorCode)
		}
	}

	msg := &xproto.MsgEnterTableResult{}
	var status32 = int32(errorCode)
	msg.Status = &status32

	buf, err := formatGameMsg(cl, msg, int32(xproto.MessageCode_OPPlayerEnterTable))
	if err != nil {
		cl.Println("SendEnterTableResult, failed, formatGameMsg error:", err)
		return
	}

	if ws != nil {
		ws.WriteMessage(websocket.BinaryMessage, buf)
	} else {
		cl.Println("SendEnterTableResult, ws == nil")
	}
}

func formatGameMsg(cl *log.Entry, pb proto.Message, code int32) ([]byte, error) {
	gmsg := &xproto.GameMessage{}
	gmsg.Code = &code

	if pb != nil {
		bytes, err := proto.Marshal(pb)

		if err != nil {
			cl.Panic("marshal msg failed:", err)
		}
		gmsg.Data = bytes
	}

	return proto.Marshal(gmsg)
}

// LoadTableConfigFromRedis load config string from redis
func LoadTableConfigFromRedis(tableConfigID string, cl *log.Entry) string {
	return ""
}
