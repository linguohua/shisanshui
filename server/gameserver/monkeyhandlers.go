// 用于支持配牌器操作服务器的函数

package gameserver

import (
	"fmt"
	"net/http"
	"shisanshui/redishelper"
	"shisanshui/tables"
	"strconv"

	"github.com/gomodule/redigo/redis"
	"github.com/julienschmidt/httprouter"
	log "github.com/sirupsen/logrus"
)

type monkeyHandleFunc func(w http.ResponseWriter, r *http.Request, cl *log.Entry)

// onCreateMonkeyTable 创建一个配牌器使用的牌桌
func onCreateMonkeyTable(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	query := r.URL.Query()
	tableID := query.Get("tableID")

	err := tables.MonkeyCreateMonkeyTable(tableID, cl)
	if err != nil {
		w.WriteHeader(404)
		w.Write([]byte(err.Error()))
	}
}

// onDestroyMonkeyTable 销毁配牌器使用的牌桌
func onDestroyMonkeyTable(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	query := r.URL.Query()
	tableID := query.Get("tableID")

	err := tables.MonkeyDestroyMonkeyTable(tableID, cl)
	if err != nil {
		w.WriteHeader(404)
		w.Write([]byte(err.Error()))
	}
}

// onExportTableOperations 导出用户当前所在的牌桌的打牌记录
func onExportTableOperations(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	// var userID = r.URL.Query().Get("userID")
	// var recordSID = ""
	// if userID == "" {
	// 	recordSID = r.URL.Query().Get("recordSID")
	// 	if recordSID == "" {
	// 		w.WriteHeader(404)
	// 		w.Write([]byte("must supply userID or recordSID"))
	// 		return
	// 	}
	// }

	// if userID != "" {
	// 	exportTableOperationsByUserID(w, r, userID)
	// } else {
	// 	exportTableOperationsByRecordSID(w, r, recordSID)
	// }
}

// onExportTableReplayRecordsSIDs 导出某个牌桌所有的回播记录的ID列表
func onExportTableReplayRecordsSIDs(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	// recordSID := r.URL.Query().Get("recordSID")
	// if recordSID == "" {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("must supply userID or recordSID"))
	// 	return
	// }

	// conn := pool.Get()
	// defer conn.Close()

	// recordID, err := redis.String(conn.Do("HGET", stateless.MJRecorderTablePrefix+recordSID, "rid"))
	// if err != nil && err != redis.ErrNil {
	// 	log.Println("can't found rid for sid:", recordSID)
	// 	w.Write([]byte("no mj record found for record:" + recordSID))
	// 	return
	// }

	// // 新的代码已经把sharedID放在MJRecorderShareIDTable哈希表中
	// if recordID == "" {
	// 	recordID, err = redis.String(conn.Do("HGET", stateless.MJRecorderShareIDTable, recordSID))
	// 	if err != nil {
	// 		log.Println("can't found rid for sid:", recordSID)
	// 		w.Write([]byte("no mj record found for record:" + recordSID))
	// 		return
	// 	}
	// }

	// buf, err := loadMJTableRecardShareIDs(conn, recordID)
	// if err != nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("no mj record found for record:" + err.Error()))
	// 	return
	// }

	// w.Write(buf)
}

// exportTableOperationsByRecordSID 导出某个回播记录
func exportTableOperationsByRecordSID(w http.ResponseWriter, r *http.Request, recordSID string, cl *log.Entry) {
	// conn := pool.Get()
	// defer conn.Close()

	// recordID, err := redis.String(conn.Do("HGET", stateless.MJRecorderTablePrefix+recordSID, "rid"))
	// if err != nil && err != redis.ErrNil {
	// 	log.Println("can't found rid for sid:", recordSID)
	// 	w.Write([]byte("no mj record found for record:" + recordSID))
	// 	return
	// }

	// // 新的代码已经把sharedID放在MJRecorderShareIDTable哈希表中
	// if recordID == "" {
	// 	recordID, err = redis.String(conn.Do("HGET", stateless.MJRecorderShareIDTable, recordSID))
	// 	if err != nil {
	// 		log.Println("can't found rid for sid:", recordSID)
	// 		w.Write([]byte("no mj record found for record:" + recordSID))
	// 		return
	// 	}
	// }

	// buf := loadMJRecord(conn, recordID)

	// if buf == nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("no mj record found for record:" + recordSID))
	// 	return
	// }

	// w.Write(buf)
}

// exportTableOperationsByUserID 导出某个玩家最新的回播记录
func exportTableOperationsByUserID(w http.ResponseWriter, r *http.Request, userID string, cl *log.Entry) {
	// var buf []byte
	// user, ok := usersMap[userID]
	// if ok {
	// 	// 先尝试加载其所在的房间的操作列表
	// 	var table = user.user.getTable()

	// 	if table != nil {
	// 		log.Println("user in server, table ID:", table.ID)
	// 		switch table.state.(type) {
	// 		case *SPlaying:
	// 			s := table.state.(*SPlaying)
	// 			ctx := s.lctx
	// 			if ctx != nil {
	// 				log.Println("found active ctx in table:", table.ID)
	// 				buf = ctx.toByteArray()
	// 			}
	// 			break
	// 		default:
	// 			break
	// 		}
	// 	}
	// }

	// if buf == nil {
	// 	log.Println("can't found active record, try to load from redis")
	// 	buf = loadMJLastRecordForUser(userID)
	// }

	// if buf == nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("no mj record found for user:" + userID))
	// 	return
	// }

	// w.Write(buf)
}

// onExportTableCfg 导出牌桌的配置
func onExportTableCfg(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	var tableConfigID = r.URL.Query().Get("tableConfigID")
	if tableConfigID == "" {
		w.WriteHeader(404)
		w.Write([]byte("must supply tableConfigID"))
		return
	}

	buf := tables.LoadTableConfigFromRedis(tableConfigID, cl)
	if len(buf) < 1 {
		w.WriteHeader(404)
		w.Write([]byte("failed to load config for:" + tableConfigID))
		return
	}

	w.Write([]byte(buf))
}

// onTableKickAll 强制牌桌玩家退出
func onTableKickAll(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	var tableNumber = r.URL.Query().Get("tableNumber")
	if tableNumber == "" {
		w.WriteHeader(404)
		w.Write([]byte("must supply tableNumber"))
		return
	}

	err := tables.MonkeyKickAll(tableNumber, cl)
	if err != nil {
		w.WriteHeader(404)
		w.Write([]byte(err.Error()))
	} else {
		w.Write([]byte("OK, kick out all in table:" + tableNumber))
	}
}

// onTableReset 重置牌桌
func onTableReset(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	// var tableNumber = r.URL.Query().Get("tableNumber")
	// if tableNumber == "" {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("must supply tableNumber"))
	// 	return
	// }

	// table := tableMgr.getTableByNumber(tableNumber)
	// if table == nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("no table for :" + tableNumber))
	// 	return
	// }

	// table.reset()

	// w.Write([]byte("OK, reset table:" + tableNumber))
}

// onTableDisband 解散牌桌
func onTableDisband(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	// log.Println("monkey try to disband table...")
	// var tableNumber = r.URL.Query().Get("tableNumber")
	// if tableNumber == "" {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("must supply tableNumber"))
	// 	return
	// }

	// table := tableMgr.getTableByNumber(tableNumber)
	// if table == nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("no table for :" + tableNumber))
	// 	return
	// }

	// tableMgr.forceDisbandTable(table, pokerface.TableDeleteReason_DisbandBySystem)

	// w.Write([]byte("OK, disband table:" + tableNumber))
}

// onExportUserLastRecord 导出某个玩家的最后回播记录
func onExportUserLastRecord(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	// var userID = r.URL.Query().Get("userID")
	// buf := loadMJLastRecordForUser(userID)
	// if buf == nil {
	// 	w.WriteHeader(404)
	// 	w.Write([]byte("failed to load record for userID:" + userID))
	// 	return
	// }

	// w.Write(buf)
}

// onQueryTableCount 查询房间个数
func onQueryTableCount(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	str := tables.MonkeyQueryTableCount(cl)

	w.Write([]byte(str))
}

// onQueryUserCount 查询玩家个数
func onQueryUserCount(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	userCount := tables.GetMgr().PlayerCount()
	w.Write([]byte(strconv.Itoa(int(userCount))))
}

// onAttachDealCfg2Table 附加发牌配置到牌桌
func onAttachDealCfg2Table(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	body := make([]byte, r.ContentLength)
	defer r.Body.Close()
	n, _ := r.Body.Read(body)
	if n != int(r.ContentLength) {
		cl.Println("attach deal cfg error, read message length not match content length")
		return
	}

	tableNumber := r.URL.Query().Get("tableNumber")
	err := tables.MonkeyAttachDealCfg2Table(tableNumber, string(body), cl)
	if err != nil {
		w.WriteHeader(404)
		w.Write([]byte(err.Error()))
	}
}

// onAttachTableCfg2Table 附加牌桌配置到牌桌
func onAttachTableCfg2Table(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	body := make([]byte, r.ContentLength)
	defer r.Body.Close()
	n, _ := r.Body.Read(body)

	if n != int(r.ContentLength) {
		cl.Println("attach table cfg error, read message length not match content length")
		return
	}

	tableNumber := r.URL.Query().Get("tableNumber")
	err := tables.MonkeyAttachTableCfg2Table(tableNumber, string(body), cl)
	if err != nil {
		w.WriteHeader(404)
		w.Write([]byte(err.Error()))
	}
}

// onKickUser 强制某个玩家离开牌桌
func onKickUser(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	query := r.URL.Query()
	var userID = query.Get("userID")
	tableNumber := query.Get("tableNumber")

	err := tables.MonkeyKickout(tableNumber, userID, cl)
	if err == nil {
		w.Write([]byte("kickout ok, ID:" + userID))
	} else {
		w.Write([]byte(err.Error()))
	}
}

// onQueryTableExceptionCount 查询发生异常的房间个数
func onQueryTableExceptionCount(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	w.Write([]byte(fmt.Sprintf("%d", tables.GetMgr().ExceptionCount())))
}

// onClearTableExceptionCount 重置异常计数为0
func onClearTableExceptionCount(w http.ResponseWriter, r *http.Request, cl *log.Entry) {
	tables.GetMgr().ClearExceptionCount()
}

// authMonkeyHandle 包装响应函数，加上账号密码验证，并初始化log entry
func authMonkeyHandle(origin monkeyHandleFunc) httprouter.Handle {
	return func(w http.ResponseWriter, r *http.Request, _ httprouter.Params) {
		cl := log.WithField("peer", retrieveClientAddr(r))
		query := r.URL.Query()
		var account = query.Get("account")
		var password = query.Get("password")
		cl.Printf("authMonkeyHandle monkey access, path:%s", r.URL.Path)
		conn := redishelper.GetConn()
		defer conn.Close()

		tableName := fmt.Sprintf("%s", "monkey-auth")
		pass, e := redis.String(conn.Do("HGET", tableName, account))
		if e != nil || password != pass {
			w.Header().Set("WWW-Authenticate", "Basic realm=Restricted")
			http.Error(w, http.StatusText(http.StatusUnauthorized), http.StatusUnauthorized)
		} else {
			origin(w, r, cl)
		}
	}
}

func registerMonkeyHandlers() {
	monkeyPath := rootPath + "/monkey"
	rootRouter.POST(monkeyPath+"/create-monkey-table", authMonkeyHandle(onCreateMonkeyTable))
	rootRouter.POST(monkeyPath+"/destroy-monkey-table", authMonkeyHandle(onDestroyMonkeyTable))
	rootRouter.POST(monkeyPath+"/attach-deal-cfg", authMonkeyHandle(onAttachDealCfg2Table))
	rootRouter.POST(monkeyPath+"/attach-table-cfg", authMonkeyHandle(onAttachTableCfg2Table))
	rootRouter.POST(monkeyPath+"/kick-user", authMonkeyHandle(onKickUser))
	rootRouter.GET(monkeyPath+"/export-table-ops", authMonkeyHandle(onExportTableOperations))
	rootRouter.GET(monkeyPath+"/export-table-cfg", authMonkeyHandle(onExportTableCfg))
	rootRouter.GET(monkeyPath+"/export-user-last-record", authMonkeyHandle(onExportUserLastRecord))
	rootRouter.POST(monkeyPath+"/kick-all", authMonkeyHandle(onTableKickAll))
	rootRouter.POST(monkeyPath+"/reset-table", authMonkeyHandle(onTableReset))
	rootRouter.POST(monkeyPath+"/disband-table", authMonkeyHandle(onTableDisband))
	rootRouter.GET(monkeyPath+"/table-count", authMonkeyHandle(onQueryTableCount))
	rootRouter.GET(monkeyPath+"/user-count", authMonkeyHandle(onQueryUserCount))
	rootRouter.GET(monkeyPath+"/table-exception", authMonkeyHandle(onQueryTableExceptionCount))
	rootRouter.POST(monkeyPath+"/clear-table-exception", authMonkeyHandle(onClearTableExceptionCount))
	rootRouter.GET(monkeyPath+"/export-table-share-record-ids", authMonkeyHandle(onExportTableReplayRecordsSIDs))
}
