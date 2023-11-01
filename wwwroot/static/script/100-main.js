
Interactive.init()

const hash = location.hash

InteractiveAction.gotoPage(
    (hash && hash.length)
        ? hash.substring(1)
        : `dashboard`
)
