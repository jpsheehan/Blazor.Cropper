const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

function loadHelper(blob) {
    const calls = {};
    const context = vm.createContext({
        Uint8Array,
        document: {
            createElement(tag) {
                assert.equal(tag, 'canvas');
                return {
                    getContext: () => ({ drawImage: (...args) => { calls.draw = args; } }),
                    toBlob(callback, format, quality) {
                        calls.format = format;
                        calls.quality = quality;
                        callback(blob);
                    }
                };
            }
        }
    });
    vm.runInContext(readFileSync(path.join(__dirname, '../Blazor.Cropper/wwwroot/CropHelper.js'), 'utf8'), context);
    return { context, calls };
}

test('browser crop returns bytes and actual MIME type on .NET 10', async () => {
    const blob = { type: 'image/png', arrayBuffer: async () => Uint8Array.from([1, 2, 3]).buffer };
    const { context, calls } = loadHelper(blob);
    context.setVersion(10);
    const result = await context.cropAsync('image', 1, 2, 3, 4, 0, 0, 6, 8, 'image/webp', 75);
    assert.deepEqual(Array.from(result.data), [1, 2, 3]);
    assert.equal(result.mimeType, 'image/png');
    assert.deepEqual(calls.draw, ['image', 1, 2, 3, 4, 0, 0, 6, 8]);
    assert.equal(calls.format, 'image/webp');
    assert.equal(calls.quality, 0.75);
});

test('browser crop rejects a failed encoder', async () => {
    const { context } = loadHelper(null);
    await assert.rejects(context.cropAsync('image', 0, 0, 1, 1, 0, 0, 1, 1, 'image/png', 100),
        /could not be encoded/);
});

test('closing one cropper removes its listeners and preserves another cropper', () => {
    const { context } = loadHelper(null);
    const active = new Set();
    const target = {
        addEventListener: (_type, listener) => active.add(listener),
        removeEventListener: (_type, listener) => active.delete(listener)
    };
    Object.assign(context.document, target);
    context.window = target;
    const helper = { invokeMethodAsync: async () => {} };
    context.addCropperEventListeners(helper, 'first');
    context.addCropperEventListeners(helper, 'second');
    assert.equal(active.size, 10);
    context.addCropperEventListeners(helper, 'second');
    assert.equal(active.size, 10);
    context.rmCropperEventListeners('first');
    assert.equal(active.size, 5);
    context.rmCropperEventListeners('second');
    assert.equal(active.size, 0);
    context.rmCropperEventListeners('second');
});
