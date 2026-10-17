package util

import (
	"sync"
	"testing"
	"time"
)

func TestTimerActiveTime(t *testing.T) {
	now := time.Date(2026, 10, 17, 10, 0, 0, 0, time.UTC)
	timer := NewTimer(int64(3 * time.Second))
	timer.now = func() time.Time { return now }
	assertUsed := func(want time.Duration) {
		t.Helper()
		if got := timer.Used(); got != int64(want) {
			t.Fatalf("Used() = %d, want %d", got, want)
		}
	}
	// A resolve error before Start must never add Unix-epoch age.
	timer.Pause()
	assertUsed(3 * time.Second)
	timer.Start()
	now = now.Add(2 * time.Second)
	timer.Start()
	assertUsed(5 * time.Second)
	timer.Pause()
	now = now.Add(time.Hour)
	timer.Pause()
	assertUsed(5 * time.Second)
	timer.Start()
	now = now.Add(time.Second)
	timer.Pause()
	assertUsed(6 * time.Second)
}

func TestTimerNegativeRestore(t *testing.T) {
	if got := NewTimer(-1).Used(); got != 0 {
		t.Fatalf("negative restored duration = %d", got)
	}
}

func TestTimerConcurrentProgressAndPause(t *testing.T) {
	timer := NewTimer(0)
	var wg sync.WaitGroup
	for i := 0; i < 8; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for j := 0; j < 100; j++ {
				timer.Start()
				timer.Used()
				timer.Pause()
			}
		}()
	}
	wg.Wait()
	if timer.Used() < 0 {
		t.Fatal("active time cannot be negative")
	}
}
