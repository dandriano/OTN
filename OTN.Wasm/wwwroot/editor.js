window.otnEditor = {
    point: (canvas, clientX, clientY) => {
        const rect = canvas.getBoundingClientRect();
        return [(clientX - rect.left) * 1000 / rect.width, (clientY - rect.top) * 680 / rect.height];
    }
};
