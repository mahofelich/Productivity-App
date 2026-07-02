// ============ Theme ============
window.phTheme = {
    get() {
        return document.documentElement.dataset.theme || 'dark';
    },
    set(theme) {
        document.documentElement.dataset.theme = theme;
        try { localStorage.setItem('ph-theme', theme); } catch { }
    },
    init() {
        let t = 'dark';
        try { t = localStorage.getItem('ph-theme') || 'dark'; } catch { }
        document.documentElement.dataset.theme = t;
        return t;
    },
    toggle() {
        const next = this.get() === 'dark' ? 'light' : 'dark';
        this.set(next);
        return next;
    }
};

// ============ Charts ============
const _charts = {};
function _cssVar(name, fallback) {
    const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return v || fallback;
}

window.phCharts = {
    render(id, type, labels, datasets, opts) {
        const el = document.getElementById(id);
        if (!el) return;
        if (_charts[id]) { _charts[id].destroy(); }
        Chart.defaults.color = _cssVar('--muted', '#888');
        Chart.defaults.font.family = 'Inter, sans-serif';
        _charts[id] = new Chart(el, {
            type,
            data: { labels, datasets },
            options: Object.assign({
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: (datasets.length > 1) } },
                scales: type === 'doughnut' ? {} : { y: { beginAtZero: true } }
            }, opts || {})
        });
    },
    line(id, labels, data, color) {
        this.render(id, 'line', labels, [{
            data, borderColor: color, backgroundColor: color + '22',
            fill: true, tension: 0.35, pointRadius: labels.length > 40 ? 0 : 3
        }]);
    },
    bar(id, labels, data, color) {
        this.render(id, 'bar', labels, [{ data, backgroundColor: color, borderRadius: 6 }]);
    },
    doughnut(id, labels, data, colors) {
        this.render(id, 'doughnut', labels, [{ data, backgroundColor: colors, borderWidth: 0 }],
            { cutout: '62%', plugins: { legend: { display: true, position: 'bottom' } } });
    }
};

// ============ Calendar ============
let _cal = null;
window.phCalendar = {
    render(id, events, dotNetRef) {
        const el = document.getElementById(id);
        if (!el) return;
        if (_cal) { _cal.destroy(); }
        const isNarrow = window.innerWidth < 760;
        _cal = new FullCalendar.Calendar(el, {
            initialView: isNarrow ? 'listWeek' : 'dayGridMonth',
            height: 'auto',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: isNarrow ? 'listWeek,timeGridDay' : 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
            },
            editable: true,
            events,
            eventClick(info) {
                dotNetRef.invokeMethodAsync('OnEventClick', info.event.id);
            },
            eventDrop(info) {
                dotNetRef.invokeMethodAsync('OnEventMove', info.event.id,
                    info.event.start ? info.event.start.toISOString() : null,
                    info.event.end ? info.event.end.toISOString() : null);
            },
            eventResize(info) {
                dotNetRef.invokeMethodAsync('OnEventMove', info.event.id,
                    info.event.start ? info.event.start.toISOString() : null,
                    info.event.end ? info.event.end.toISOString() : null);
            }
        });
        _cal.render();
    }
};

// ============ Kanban drag (SortableJS) ============
window.phKanban = {
    init(dotNetRef) {
        document.querySelectorAll('.kanban-col .col-body').forEach(col => {
            if (col._sortable) col._sortable.destroy();
            col._sortable = Sortable.create(col, {
                group: 'tasks',
                animation: 150,
                onEnd(evt) {
                    const id = evt.item.getAttribute('data-task-id');
                    const state = evt.to.getAttribute('data-state');
                    dotNetRef.invokeMethodAsync('OnTaskMoved', parseInt(id), parseInt(state));
                }
            });
        });
    }
};

// ============ Markdown ============
window.phMarkdown = {
    render(text) {
        try { return marked.parse(text || ''); } catch { return text || ''; }
    }
};

// ============ Misc ============
window.phNotify = (title, body) => {
    try {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, { body });
        }
    } catch { }
};
window.phRequestNotify = () => {
    try { if ('Notification' in window) Notification.requestPermission(); } catch { }
};
