from utop.api.app import health


def run() -> None:
    print("UTOP service bootstrap complete.")
    print(health())


if __name__ == "__main__":
    run()
