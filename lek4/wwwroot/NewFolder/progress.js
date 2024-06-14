window.initializeProgressBar = (progress) => {
    var bar = new ProgressBar.Circle('#progress-container', {
        color: '#aaa',
        strokeWidth: 6,
        trailWidth: 1,
        easing: 'easeInOut',
        duration: 1400,
        text: {
            autoStyleContainer: false
        },
        from: { color: '#aaa', width: 1 },
        to: { color: '#333', width: 6 },
        step: function (state, circle) {
            circle.path.setAttribute('stroke', state.color);
            circle.path.setAttribute('stroke-width', state.width);

            var value = Math.round(circle.value() * 100);
            if (value === 0) {
                circle.setText('');
            } else {
                circle.setText(value);
            }
        }
    });
    bar.text.style.fontFamily = '"Raleway", Helvetica, sans-serif';
    bar.text.style.fontSize = '2rem';

    bar.animate(progress);  // Number from 0.0 to 1.0
};

window.updateProgressBar = (progress) => {
    if (window.bar) {
        window.bar.animate(progress);
    }
};
