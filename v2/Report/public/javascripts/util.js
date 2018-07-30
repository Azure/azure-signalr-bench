function transparentize(color, opacity) {
    var alpha = opacity === undefined ? 0.5 : 1 - opacity;
    return Color(color).alpha(alpha).rgbString();
}

function parseScenarioLabel(scenario) {
    return scenario.split("_").map(w => w.charAt(0).toUpperCase() + w.substr(1)).join(" ");
}

function parseTime(timestring) {
    // sample: 2018-06-07T05:58:06Z
    return timestring.split('T')[1].split('Z')[0]
}