package tables

import (
	"fmt"
	"shisanshui/xproto"

	log "github.com/sirupsen/logrus"
)

// MonkeyKickout kickout a player
func MonkeyKickout(tableNumber string, userID string, cl *log.Entry) error {
	var err error
	table := mgr.GetTableByNumber(tableNumber)
	if table == nil {
		err = fmt.Errorf("MonkeyKickout, no table found for :" + tableNumber)
		cl.Println(err)
		return err
	}

	table.HoldLock(func() {
		err = table.kickPlayer(userID)
	})

	return err
}

// MonkeyCreateMonkeyTable attach table config to monkey table
func MonkeyCreateMonkeyTable(tableID string, cl *log.Entry) error {
	if tableID == "" {
		tableID = "monkey-table"
	}

	var tableConfig = tableConfigNew()

	// 创建一个monkey房间
	table := newNewForMonkey(tableID, tableID, tableConfig)

	err := mgr.addTable(table)
	if err != nil {
		cl.Printf("create monkey table with ID:%s failed: %v", tableID, err)
	} else {
		cl.Printf("create monkey table with ID:%s succeed", tableID)
	}

	return err
}

// MonkeyDestroyMonkeyTable destroy table
func MonkeyDestroyMonkeyTable(tableID string, cl *log.Entry) error {
	table := mgr.GetTable(tableID)
	if table == nil {
		cl.Printf("MonkeyDestroyMonkeyTable failed, table not found:%s", tableID)
		return fmt.Errorf("monkey table not found:%s", tableID)
	}

	table.HoldLock(func() {
		table.destroy(xproto.TableDeleteReason_DisbandBySystem)
	})

	mgr.delTable(table)

	return nil
}

// MonkeyAttachTableCfg2Table attach table config to monkey table
func MonkeyAttachTableCfg2Table(tableNumber string, body string, cl *log.Entry) error {
	if tableNumber == "" {
		return fmt.Errorf("must supply tableNumber")
	}

	table := mgr.GetTableByNumber(tableNumber)
	if table == nil {
		cl.Printf("MonkeyAttachTableCfg2Table, no table found for table number:%s", tableNumber)
		return fmt.Errorf("no table found for table number:%s", tableNumber)
	}

	var cfg, errS = monkeyConfigNewFromCSV(body)
	if errS != nil {
		return errS
	}

	var err error

	table.HoldLock(func() {
		var stateConst = table.state.name()
		// 要求的玩家数量不一致
		if cfg.playerCount() < table.config.PlayerNumAcquired || cfg.playerCount() > table.config.PlayerNumMax {
			err = fmt.Errorf("player number not match, table require:%d, max:%d, but cfg:%d", table.config.PlayerNumAcquired,
				table.config.PlayerNumMax,
				cfg.playerCount())

			cl.Println(err)
			return
		}

		// 如果是强制要求顺序，则必须所有的userID不能为空
		if cfg.isForceConsistent {
			for i, tc := range cfg.monkeyUserCardsCfgList {
				if tc.userID == "" {
					err = fmt.Errorf("player %d not supply userID", i)
					cl.Println(err)
					return
				}
			}
		}

		switch stateConst {
		case "idle":
			break
		case "playing":
			err = fmt.Errorf("table is playing state, not allow to attach deal cfg")
			break
		case "waiting":
			if cfg.playerCount() < len(table.players) {
				err = fmt.Errorf("current player number is large than config's")
				break
			}

			if cfg.isForceConsistent {
				for i, p := range table.players {
					if p.ID != cfg.monkeyUserCardsCfgList[i].userID {
						err = fmt.Errorf("player userID or sequence not match")
						break
					}
				}
			}

			break
		}

		if err == nil {
			cl.Printf("bind deal cfg to table:%s, cfg:%s\n", tableNumber, cfg.name)
			table.monkeyCfg = cfg
		}
	})

	return err
}

// MonkeyAttachDealCfg2Table attach table config to monkey table
func MonkeyAttachDealCfg2Table(tableNumber string, body string, cl *log.Entry) error {
	if tableNumber == "" {
		return fmt.Errorf("table number is empty")
	}

	table := mgr.GetTableByNumber(tableNumber)
	if table == nil {
		return fmt.Errorf("no table found for table number:%s", tableNumber)
	}

	tableConfig, err := tableConfigNewFromJSON(body)
	if err != nil {
		return err
	}

	cl.Printf("bind table cfg to table:%s, cfg:%+v\n", tableNumber, tableConfig)
	table.HoldLock(func() {
		table.config = tableConfig
	})

	return nil
}

// MonkeyQueryTableCount query table count
func MonkeyQueryTableCount(cl *log.Entry) string {
	tableCount := 0
	tableIdle := 0
	tableWaiting := 0
	tablePlaying := 0

	mgr.tablesMap.Range(func(_ interface{}, value interface{}) bool {
		table := value.(*Table)

		if table != nil {
			tableCount++
			stateConst := table.state.name()
			switch stateConst {
			case "idle":
				tableIdle++
				break
			case "waiting":
				tableWaiting++
				break
			case "playing":
				tablePlaying++
				break
			}
		}
		return true
	})

	return fmt.Sprintf("table count:%d, idle:%d, wait:%d, play:%d", tableCount, tableIdle, tableWaiting, tablePlaying)
}

// MonkeyKickAll killout all players of table
func MonkeyKickAll(tableNumber string, cl *log.Entry) error {
	table := mgr.GetTableByNumber(tableNumber)
	if table == nil {
		err := fmt.Errorf("no table for :" + tableNumber)
		cl.Println(err)
		return err
	}

	table.HoldLock(func() {
		table.kickAll()
	})

	return nil
}
