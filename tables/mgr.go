package tables

import (
	"sync"
)

var (
	// mgr table manager singleton instance
	mgr *TableMgr = &TableMgr{}
)

// TableMgr table manager
// 保存所有已经创建的牌桌，可以根据牌桌的uuid查找牌桌，也可以根据
// 牌桌的数字编号查找牌桌
type TableMgr struct {
	tablesMap   sync.Map
	tablesIDMap sync.Map
}

// GetTableByNumber get table by table number
func (m *TableMgr) GetTableByNumber(tableNumber string) *Table {
	tuid, ok := m.tablesIDMap.Load(tableNumber)
	if ok {
		return m.GetTable(tuid.(string))
	}

	return nil
}

// GetTable get table by uuid
func (m *TableMgr) GetTable(tableID string) *Table {
	t, ok := m.tablesMap.Load(tableID)
	if ok {
		return t.(*Table)
	}

	return nil
}

// GetMgr retrieve table manager instance
func GetMgr() *TableMgr {
	return mgr
}
