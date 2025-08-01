This plugin allows players to join a full server and wait in a queue until a slot becomes available.

Config:
```
# Whether the plugin is enabled.
is_enabled: true
# Enable debug logs.
debug: false
queue_hint_message: |-
  <align="center"><color=orange><size=25>⚠️ The Server is Full! ⚠️</size></color>
  <color=#A9A9A9><size=23>But don’t worry! You are waiting in a queue!</size></color>
  <color=#DCDCDC><size=21>Players waiting:</color> <color=#ADFF2F>{queue_count}</size></color>

  <size=20>Your position in the queue: <color=#00FF00>#{position}</color></size>

  <size=18>Round time <color=#2F4F4F>»</color> <color=#C0C0C0>{round_time} elapsed</color>
queue_leave_hint_message: <color=green>You will now join!</color>
# Priority order of queue groups. Higher in the list = higher priority.
queue_group_priority:
- moderator
- owner
# Allows NW Staff to always skip the queue.
allow_n_w_staff_to_skip_queue: true
```

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/460bc523-1155-4da9-bd2e-f1854a4f5e6a" />
