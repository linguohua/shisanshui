package tables

import (
	"fmt"
	"sync"
	"sync/atomic"
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

	userIDOwners    sync.Map
	userIDOwnersLen uint32

	exceptionCount uint32
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

func (m *TableMgr) addTable(table *Table) error {
	_, loaded := m.tablesMap.LoadOrStore(table.UUID, table)
	if loaded {
		return fmt.Errorf("add Table failed: duplicate uuid %s", table.UUID)
	}

	m.tablesIDMap.Store(table.Number, table.UUID)

	return nil
}

func (m *TableMgr) delTable(table *Table) {
	m.tablesIDMap.Delete(table.Number)
	m.tablesMap.Delete(table.UUID)
}

func (m *TableMgr) delTableByID(tableID string) error {
	t := m.GetTable(tableID)
	if t != nil {
		return fmt.Errorf("delTableByID failed: no table found for id:%s", tableID)
	}

	m.delTable(t)

	return nil
}

func (m *TableMgr) delTableByNumber(tableNumber string) error {
	t := m.GetTableByNumber(tableNumber)
	if t != nil {
		return fmt.Errorf("delTableByID failed: no table found for number:%s", tableNumber)
	}

	m.delTable(t)

	return nil
}

// ClearExceptionCount reset excetpion counter
func (m *TableMgr) ClearExceptionCount() {
	atomic.StoreUint32(&m.exceptionCount, 0)
}

// IncExceptionCount add 1 to exception counter
func (m *TableMgr) IncExceptionCount() {
	atomic.AddUint32(&m.exceptionCount, 1)
}

// ExceptionCount get exception counter
func (m *TableMgr) ExceptionCount() uint32 {
	return atomic.LoadUint32(&m.exceptionCount)
}

// PlayerCount get player count
func (m *TableMgr) PlayerCount() uint32 {
	return atomic.LoadUint32(&m.userIDOwnersLen)
}

func (m *TableMgr) addUserIDOwner(userID string, table *Table) {
	m.userIDOwners.Store(userID, table)
	// NOTE: if m.userIDOwners already contains the same key, then we
	// will have error here: the len of userIDOwners will different from
	// userIDOwnersLen
	atomic.AddUint32(&m.userIDOwnersLen, 1)
}

// GetMgr retrieve table manager instance
func GetMgr() *TableMgr {
	return mgr
}
