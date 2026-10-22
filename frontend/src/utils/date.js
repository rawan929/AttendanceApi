// Backend sends UTC ISO strings; show them in the browser's local time
export const toLocal = d => new Date(typeof d === 'string' && !d.endsWith('Z') ? d + 'Z' : d).toLocaleString()
