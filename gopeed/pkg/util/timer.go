package util

import (
	"sync"
	"time"
)

type Timer struct {
	mu      sync.Mutex
	started time.Time
	used    time.Duration
	now     func() time.Time
}

func NewTimer(used int64) *Timer {
	if used < 0 {
		used = 0
	}
	return &Timer{used: time.Duration(used), now: time.Now}
}

func (t *Timer) Start() {
	t.mu.Lock()
	defer t.mu.Unlock()
	if t.started.IsZero() {
		t.started = t.now()
	}
}

func (t *Timer) Pause() {
	t.mu.Lock()
	defer t.mu.Unlock()
	if !t.started.IsZero() {
		t.used += t.now().Sub(t.started)
		t.started = time.Time{}
	}
}

func (t *Timer) Used() int64 {
	t.mu.Lock()
	defer t.mu.Unlock()
	used := t.used
	if !t.started.IsZero() {
		used += t.now().Sub(t.started)
	}
	return int64(used)
}
