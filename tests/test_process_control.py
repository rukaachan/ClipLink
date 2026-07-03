from cliplink.process_control import clear_pid, is_worker_running, read_pid, write_pid


def test_pid_file_round_trip(tmp_path):
    write_pid(tmp_path, 12345)

    assert read_pid(tmp_path) == 12345

    clear_pid(tmp_path)
    assert read_pid(tmp_path) is None


def test_is_worker_running_false_for_missing_pid(tmp_path):
    assert is_worker_running(tmp_path) is False
