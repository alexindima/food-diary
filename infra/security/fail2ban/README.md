# FoodDiary fail2ban

These filters ban noisy commodity scanners that repeatedly hit known sensitive paths through nginx.

Install on a server:

```bash
sudo cp infra/security/fail2ban/filter.d/nginx-fooddiary-scanners.conf /etc/fail2ban/filter.d/
sudo cp infra/security/fail2ban/jail.d/nginx-fooddiary-scanners.conf /etc/fail2ban/jail.d/
sudo systemctl enable --now fail2ban
sudo fail2ban-client reload
sudo fail2ban-client status nginx-fooddiary-scanners
```

The jail assumes nginx writes host-mounted logs to:

```text
/var/log/nginx/fooddiary.access.log
/var/log/nginx/fooddiary.admin.access.log
```

Keep backend-only Docker ports bound to `127.0.0.1`; fail2ban is not a substitute for closing public Redis, RabbitMQ, and internal HTTP ports.
