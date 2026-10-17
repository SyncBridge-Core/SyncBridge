// Runs the shipped client in a minimal DOM harness; no external dependencies.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const script = fs.readFileSync(path.join(__dirname, '../src/SyncBridge.Core/wwwroot/js/shop-floor.js'), 'utf8');
function setup(fetch) {
    const cells = ['orderNumber', 'productCode', 'productName', 'recipeId', 'quantity', 'status']
        .map(field => ({ dataset: {field}, textContent: '' }));
    const element = () => ({textContent: '', classList: {add() {}, remove() {}}, setAttribute() {}});
    const input = {...element(), value: '', focus() {}, select() {}};
    const button = {disabled: false};
    let submit;
    const form = {dataset: {apiUrl: '/api/production-orders/'}, querySelector: () => button,
        addEventListener: (_, handler) => {submit = handler;}};
    const results = {...element(), hidden: true, querySelectorAll: () => cells};
    const feedback = element();
    const elements = {'order-lookup': form, 'order-number': input, 'lookup-feedback': feedback,
        'manufacturing-context': element(), 'order-context': results, 'mes-status': element()};
    vm.runInNewContext(script, {document: {getElementById: id => elements[id]}, fetch, AbortSignal});
    return {input, button, feedback, results, cells, submit: () => submit({preventDefault() {}})};
}
const dto = {orderNumber:'PO-1', productCode:'P', productName:'<img src=x onerror=alert(1)>', recipeId:'R', quantity:12.125, status:1};
test('scanner value preserved, encoded GET, text-only rendering and stale-data clearance', async () => {
    let request;
    const app = setup(async (url, options) => {request = {url, options}; return {ok:true, json:async () => dto};});
    app.input.value = ' PO/#?1 ';
    await app.submit();
    assert.equal(request.url, '/api/production-orders/%20PO%2F%23%3F1%20');
    assert.equal(request.options.method, 'GET');
    assert.equal(request.options.credentials, 'same-origin');
    assert.equal(request.options.cache, 'no-store');
    assert.equal(app.cells[2].textContent, dto.productName);
    assert.equal(app.cells[5].textContent, 'Released');
    assert.equal(app.results.hidden, false);
    app.input.value = '';
    await app.submit();
    assert.equal(app.results.hidden, true);
    assert.ok(app.cells.every(cell => cell.textContent === ''));
});
for (const value of ['', '   ', 'X'.repeat(41), 'PO\u0001']) {
    test(`invalid input ${JSON.stringify(value)} makes no request`, async () => {
        const app = setup(() => {throw new Error('must not fetch');});
        app.input.value = value;
        await app.submit();
        assert.match(app.feedback.textContent, /Invalid Production Order/);
        assert.equal(app.results.hidden, true);
    });
}
for (const [status, message] of [[400,/Invalid/], [401,/session has expired/], [403,/role/], [404,/not found/], [500,/unsuccessful/], [503,/unsuccessful/]]) {
    test(`HTTP ${status} feedback`, async () => {
        const app = setup(async () => ({ok:false, status}));
        app.input.value = 'PO-1';
        await app.submit();
        assert.match(app.feedback.textContent, message);
        assert.equal(app.results.hidden, true);
        assert.equal(app.button.disabled, false);
    });
}
for (const response of [null, {}, {...dto, status:99}, {...dto, quantity:'12'}]) {
    test(`unexpected DTO ${JSON.stringify(response)}`, async () => {
        const app = setup(async () => ({ok:true, json:async () => response}));
        app.input.value = 'PO-1';
        await app.submit();
        assert.match(app.feedback.textContent, /Unexpected response/);
        assert.equal(app.results.hidden, true);
    });
}
test('network failure stays generic and retry remains possible', async () => {
    const app = setup(async () => {throw new Error('private diagnostic');});
    app.input.value = 'PO-1';
    await app.submit();
    assert.match(app.feedback.textContent, /Unable to reach/);
    assert.equal(app.button.disabled, false);
});
test('duplicate submit is blocked while retrieval is pending', async () => {
    let calls = 0;
    let finish;
    const app = setup(() => {calls++; return new Promise(resolve => {finish = resolve;});});
    app.input.value = 'PO-1';
    const first = app.submit();
    await app.submit();
    assert.equal(calls, 1);
    finish({ok:true, json:async () => dto});
    await first;
    assert.equal(app.button.disabled, false);
});
