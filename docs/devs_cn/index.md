---
layout: article
sidebar:
    nav: devs_cn
---

# 开发者信息

## 基础

每一个悬浮窗，本质上都是一个网页程序。这意味着你需要对HTML、JavaScript和CSS有基本的了解。在此我们假设您已经掌握了这些知识。

为了演示，你可以在OverlayPlugin的URL字段中输入 https://www.baidu.com/ 或其他网址，你就会看到网址对应的网页在悬浮窗中加载出来。当然你也可以输入本地路径（或者通过`...`按钮选择本地文件）来加载本地网页。

一个比较推荐初心者查看的简易悬浮窗Demo是默认的悬浮窗样式 [miniparse.html](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/OverlayPlugin.Core/resources/miniparse.html)。

它通过JavaScript在HTML中生成基于MiniParse数据的表格，并使用了CSS来让数据变得漂亮一点。

如果你已经了解HTML、CSS和JavaScript，那么上面大部分的内容对你来说并不陌生，你应该会对如何接收MiniParse（和其他）数据更感兴趣 :)

## API

首先，你需要在悬浮窗网页中引用OverlayPlugin的[common.min.js](.../assets/shared/common.min.js)。
```html
<script type="text/javascript" src="https://overlayplugin.github.io/OverlayPlugin/assets/shared/common.min.js"></script>
```
如果你的用户位于中国境内，我们推荐引用这里的镜像文件。
```html
<script type="text/javascript" src="https://act.diemoe.net/overlays/common/common.min.js"></script>
```
[点击这个链接查看该js的未压缩原始代码](https://github.com/OverlayPlugin/OverlayPlugin/blob/main/docs/assets/shared/common.js) if you're curious.

像上面这样在网页中添加代码段落，你将始终引用最新版本的 `common.js` ，它将与最新版本的OverlayPlugin兼容。

该文件提供了一个围绕OverlayPlugin的标准Overlay与WebSocket的API封装。这意味着你只需要引用此SDK进行开发，就可以直接在OverlayPlugin中加载你的悬浮窗，或通过在浏览器中打开它并附加`?OVERLAY_WS=ws://127.0.0.1:10501/ws`参数通过WSServer传递数据使用。后者需要您先启动WebSocket服务器（通过OverlayPlugin的WSServer标签）。

以下文档仅针对 `common.js` 所声明的可用函数。

### addOverlayListener(event, callback)

这个函数的作用非常类似于`document.addEventListener(...)`。你可以为每个你想附加到一个事件的回调调用这个函数。你可以为一个事件附加任意数量的回调。

**示例:**
```javascript
addOverlayListener('CombatData', (data) => {
    console.log(`经历战斗: ${data.title} | ${data.duration} | 团伤: ${data.ENCDPS}`);
});
```

[事件类型](./event_types.md)中描述了一些可用的事件。
但请记住，附加的扩展插件可以添加更多的事件源，这些事件源可以声明自己的事件和处理程序（关于处理程序的更多信息，请参阅下面的[`callOverlayHandler`](#calloverlayhandlerparameters)）。

### removeOverlayListener(event, callback)

正如你所期望的那样，这个函数可以删除一个事件监听器。

### callOverlayHandler(parameters)

这个函数允许你调用一个悬浮窗处理程序。这些处理程序是由事件源声明的（可以是内置在OverlayPlugin中的，也可以是通过Cactbot等附加插件提供的）。

OverlayPlugin目前唯一实现的处理程序是 `getLanguage`，它允许你检索ACT的FFXIV解析插件设置中设置的游戏语言。
*TODO*: 还有更多的处理程序已经被实现，但它们暂时还没有被写入到文档中。(`getCombatants`, `saveData`, `loadData`, `say`, `broadcast` 以及一些Cactbot专用的处理程序)

**示例:**
```javascript
let language = await callOverlayHandler({ call: 'getLanguage' });
console.log(language.language, language.languageId);
```

### startOverlayEvents()

当你完成了悬浮窗事件监听器的注册之后，你可以调用这个函数。一旦这个函数被调用，OverlayPlugin将开始发送事件信息。其中一部分事件将立即触发，并提供当前状态信息，诸如 `ChangeZone` 以及 `ChangePrimaryPlayer` 之类。

